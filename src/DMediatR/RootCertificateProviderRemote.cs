using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("RootCertifier")]
    internal class RootCertificateProviderRemote : RootCertificateProvider, IRemote
    {
        public RootCertificateProviderRemote(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<RootCertificateProvider> logger)
                : base(remote, createCert, hostOptions, ioCert, logger) { }

        public override async Task<X509Certificate2> Handle(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            return await base.Handle(request, cancellationToken);
        }

        protected override async Task<X509Certificate2> GetNewCertificate(RootCertificateRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("{request}: Request certificate from remote", request.GetType().Name);
            var newcert = await this.SendRemote(request, cancellationToken);
            return newcert;
        }

        public override async Task Handle(RenewRootCertificateNotification notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // handled by NotificationForwarder
        }
    }
}