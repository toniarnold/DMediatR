using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("RootCertifier")]
    internal class RootCertificateProvider : CertificateProvider,
        IRequestHandler<RootCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewRootCertificateNotification>
    {
        private readonly CreateCertificatesClientServerAuth _createCert;
        private static SemaphoreSlim _fileLock = new(1, 1);
        protected override string? CertificateName => "Root";
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} root CA L1 certificate";

        public RootCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger
            )
                : base(remote, hostOptions, ioCert, logger)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            var locked = await request.Lock(_fileLock, cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public virtual async Task Handle(RenewRootCertificateNotification notification, CancellationToken cancellationToken)
        {
            var locked = await notification.Lock(_fileLock, cancellationToken);
            try
            {
                if (File.Exists(FileNamePfx))
                {
                    _logger.LogDebug("{notification}: renew existing certificate", notification.GetType().Name);
                    var newcert = Generate(new RootCertificateRequest() { Renew = true, HasLocked = notification.HasLocked });
                    await Save(newcert, cancellationToken);

                    // Only renewing the root certificate would leave the system
                    // in an inconsistent state, thus transitively renew the
                    // dependent child certificates immediately.
                    await Remote.Mediator.Publish(new RenewIntermediateCertificateNotification() { HasLocked = notification.HasLocked }, cancellationToken);
                }
            }
            finally { if (locked) _fileLock.Release(); }
        }

        protected async Task<X509Certificate2> RequestCertificate(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            (var loaded, var cert) = await TryLoad(cancellationToken);
            if (loaded && (request.Renew || ((cert!.NotAfter - DateTime.Now).TotalDays > Options.RenewBeforeExpirationDays)))
            {
                _logger.LogTrace("{request}: Load existing certificate", request.GetType().Name);
                return cert!;
            }
            else
            {
                request.Renew = true;
                return await GetNewCertificate(request, cancellationToken);
            }
        }

        protected virtual async Task<X509Certificate2> GetNewCertificate(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("{request}: Generate new certificate", request.GetType().Name);
            var newcert = await Generate(request, cancellationToken);
            return newcert;
        }

        private async Task<X509Certificate2> Generate(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            var newcert = Generate(request);
            await Save(newcert, cancellationToken);
            return newcert;
        }

        internal X509Certificate2 Generate(RootCertificateRequest request)
        {
            var cert = _createCert.NewRootCertificate(
                new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)(Options.RootCertificateValidDays ?? Options.ValidDays!))
                },
                3,
                Options.HostName);
#if OS_WINDOWS
#pragma warning disable CA1416
            cert.FriendlyName = FriendlyName;
#pragma warning restore CA1416
#endif
            return cert;
        }

        internal async Task Save(X509Certificate2 rootCert, CancellationToken cancellationToken)
        {
            var rootCertBytesPfx = _importExportCertificate.ExportRootPfx(Options.Password, rootCert);
            await SavePfx(rootCertBytesPfx, cancellationToken);
        }
    }
}