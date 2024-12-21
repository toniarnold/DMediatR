using CertificateManager;
using CertificateManager.Models;
using MediatR;
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
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }

        public async Task Handle(RenewRootCertificateNotification notification, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(FileName))
                {
                    _logger.LogDebug("{notification}: renew existing certificate", notification.GetType().Name);
                    var newcert = Generate(new RootCertificateRequest());
                    await Save(newcert, cancellationToken);
                }
            }
            finally { _fileLock.Release(); }
        }

        protected async Task<X509Certificate2> RequestCertificate(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            (var loaded, var cert) = await TryLoad(cancellationToken);
            if (loaded && ((cert!.NotAfter - DateTime.Now).TotalDays > Options.RenewBeforeExpirationDays))
            {
                _logger.LogDebug("{request}: load existing certificate", request.GetType().Name);
                return cert!;
            }
            else
            {
                _logger.LogDebug("{request}: generate new certificate", request.GetType().Name);
                var newcert = await Generate(request, cancellationToken);
                return newcert;
            }
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
                    ValidTo = DateTime.UtcNow.AddDays((int)Options.ValidDays!)
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
            var rootCertBytes = _importExportCertificate.ExportRootPfx(Options.Password, rootCert);
            await Save(rootCertBytes, cancellationToken);
        }
    }
}