using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("IntermediateCertifier")]
    internal class IntermediateCertificateProvider : ChainedCertificateProvider,
        IRequestHandler<IntermediateCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewIntermediateCertificateNotification>
    {
        private readonly CreateCertificatesClientServerAuth _createCert;
        private static SemaphoreSlim _fileLock = new(1, 1);
        protected override string? CertificateName => "Intermediate";
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} intermediate L2 certificate";

        public IntermediateCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger)
                : base(remote, hostOptions, ioCert, logger)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(IntermediateCertificateRequest request, CancellationToken cancellationToken)
        {
            var locked = await request.Lock(_fileLock, cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public virtual async Task Handle(RenewIntermediateCertificateNotification notification, CancellationToken cancellationToken)
        {
            var locked = await notification.Lock(_fileLock, cancellationToken);
            try
            {
                if (File.Exists(FileNamePfx))
                {
                    _logger.LogDebug("{notification}: renew existing certificate", notification.GetType().Name);
                    await base.Generate(new IntermediateCertificateRequest() { Renew = true, HasLocked = notification.HasLocked }, cancellationToken);

                    // As the intermediate certificate is used to validate
                    // client and server certificates, renewing the server
                    // certificate as last action will immediately abort this
                    // task and restart the server due to the
                    // ServerCertificateFileWatcher. The client certificates
                    // will be renewed by connecting with the old certificate to
                    // the OldPort chain.
                    await Remote.Mediator.Publish(new RenewServerCertificateNotification() { HasLocked = notification.HasLocked }, cancellationToken);
                }
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewIntermediateChainedCertificate(
                new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)(Options.IntermediateCertificateValidDays ?? Options.ValidDays!))
                },
                2,
                Options.HostName,
                parentCert);
            return cert;
        }
    }
}