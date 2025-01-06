using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("ClientCertifier")]
    internal class ClientCertificateProvider : ChainedCertificateProvider,
        IRequestHandler<ClientCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewClientCertificateNotification>
    {
        private readonly CreateCertificatesClientServerAuth _createCert;

        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        protected override string? CertificateName => "Client";
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} client L3 certificate";

        public ClientCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger)
                : base(remote, hostOptions, ioCert, logger)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(ClientCertificateRequest request, CancellationToken cancellationToken)
        {
            var locked = await request.Lock(_fileLock, cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public virtual async Task Handle(RenewClientCertificateNotification notification, CancellationToken cancellationToken)
        {
            var locked = await notification.Lock(_fileLock, cancellationToken);
            try
            {
                if (File.Exists(FileNamePfx))
                {
                    _logger.LogDebug("{notification}: renew existing certificate", notification.GetType().Name);
                    await base.Generate(new ClientCertificateRequest() { Renew = true, HasLocked = notification.HasLocked }, cancellationToken);
                }
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewClientChainedCertificate(
                new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)(Options.ClientCertificateValidDays ?? Options.ValidDays!))
                },
                Options.HostName,
                parentCert);
#if OS_WINDOWS
#pragma warning disable CA1416
            cert.FriendlyName = FriendlyName;
#pragma warning restore CA1416
#endif
            return cert;
        }
    }
}