using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal abstract class ChainedCertificateProvider : CertificateProvider
    {
        public ChainedCertificateProvider(Remote remote,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger)
                : base(remote, hostOptions, ioCert, logger)
        {
        }

        protected async Task<X509Certificate2> RequestCertificate(ChainedCertificateRequest request, CancellationToken cancellationToken)
        {
            (var loaded, var cert) = await TryLoad(cancellationToken);
            if (loaded && (request.Renew || ((cert!.NotAfter - DateTime.Now).TotalDays > Options.RenewBeforeExpirationDays)))
            {
                // Validate the chain with an additionally obtained parent
                // certificate only if not renewing the at that moment
                // inconsistent chain.
                var valid = request.Renew;
                if (!request.Renew)
                {
                    var policy = new X509ChainPolicy();
                    policy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    policy.RevocationMode = X509RevocationMode.NoCheck;
                    policy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    var parentRequest = request.ParentCertificateRequest;
                    parentRequest.Renew = true; // not transitively validate the parent here
                    parentRequest.HasLocked.UnionWith(request.HasLocked);
                    var parentCert = await Remote.Mediator.Send(parentRequest, cancellationToken);
                    policy.ExtraStore.Add(parentCert);
                    using var chain = new X509Chain() { ChainPolicy = policy };
                    valid = chain.Build(cert!);
                    if (!valid)
                    {
                        var status = from s in chain.ChainStatus select $"{s.Status}: {s.StatusInformation}";
                        string failures = System.String.Join("\n          ", status);
                        _logger.LogWarning("{request}: Existing certificate is not valid\n          {failures}", request.GetType().Name, failures);
                    }
                }
                if (valid)
                {
                    _logger.LogTrace("{request}: Load existing certificate", request.GetType().Name);
                    return cert!;
                }
            }
            // request.Renew or !valid
            request.Renew = true;
            return await GetNewCertificate(request, cancellationToken);
        }

        protected virtual async Task<X509Certificate2> GetNewCertificate(ChainedCertificateRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("{request}: Generate new certificate", request.GetType().Name);
            var newcert = await Generate(request, cancellationToken);
            return newcert;
        }

        protected async Task<X509Certificate2> Generate(ChainedCertificateRequest request, CancellationToken cancellationToken)
        {
            var parentRequest = request.ParentCertificateRequest;
            parentRequest.Renew = request.Renew;
            parentRequest.HasLocked!.UnionWith(request.HasLocked!);
            var parentCert = await Remote.Mediator.Send(parentRequest, cancellationToken);
            var newcert = Generate(request, parentCert);
            await Save(newcert, parentCert, cancellationToken);
            return newcert;
        }

        public abstract X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert);

        public async Task Save(X509Certificate2 certificate, X509Certificate2 signingCert, CancellationToken cancellationToken)
        {
            var bytesPfx = _importExportCertificate.ExportChainedCertificatePfx(Options.Password, certificate, signingCert);
            await SavePfx(bytesPfx, cancellationToken);
            await SaveCrt(certificate, cancellationToken);
        }

        public async Task SaveCrt(X509Certificate2 certificate, CancellationToken cancellationToken)
        {
            var bytesCrt = _importExportCertificate.ExportCertificatePublicKey(certificate).RawData;
            await SaveCrt(bytesCrt, cancellationToken);
        }
    }
}