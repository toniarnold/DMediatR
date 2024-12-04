using CertificateManager;
using CertificateManager.Models;
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
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} client L3 certificate";

        public ClientCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert)
                : base(remote, hostOptions, ioCert)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(ClientCertificateRequest request, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }

        async Task INotificationHandler<RenewClientCertificateNotification>.Handle(RenewClientCertificateNotification notification, CancellationToken cancellationToken)
        {
            if (File.Exists(FileName))
            {
                await base.Generate(new ClientCertificateRequest(), cancellationToken);
            }
        }

        internal override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewClientChainedCertificate(
                new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)Options.ValidDays!)
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