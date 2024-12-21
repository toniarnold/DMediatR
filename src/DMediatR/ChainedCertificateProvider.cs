using CertificateManager;
using MediatR;
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
            if (loaded && ((cert!.NotAfter - DateTime.Now).TotalDays > Options.RenewBeforeExpirationDays))
            {
                using X509Chain chain = new();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                var valid = chain.Build(cert);
                if (valid)
                {
                    _logger.LogDebug("{request}: load existing certificate", request.GetType().Name);
                    return cert;
                }
            }
            _logger.LogDebug("{request}: generate new certificate", request.GetType().Name);
            var newcert = await Generate(request, cancellationToken);
            return newcert;
        }

        protected async Task<X509Certificate2> Generate(ChainedCertificateRequest request, CancellationToken cancellationToken)
        {
            var parentCert = await Remote.Mediator.Send(request.ParentCertificateRequest, cancellationToken);
            var newcert = Generate(request, parentCert);
            await Save(newcert, parentCert, cancellationToken);
            return newcert;
        }

        internal abstract X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert);

        internal async Task Save(X509Certificate2 certificate, X509Certificate2 signingCert, CancellationToken cancellationToken)
        {
            var interCertBytes = _importExportCertificate.ExportChainedCertificatePfx(Options.Password, certificate, signingCert);
            await Save(interCertBytes, cancellationToken);
        }
    }
}