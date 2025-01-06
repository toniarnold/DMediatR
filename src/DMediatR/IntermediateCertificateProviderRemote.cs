using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("IntermediateCertifier")]
    internal class IntermediateCertificateProviderRemote : IntermediateCertificateProvider, IRemote
    {
        public IntermediateCertificateProviderRemote(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<IntermediateCertificateProvider> logger)
                : base(remote, createCert, hostOptions, ioCert, logger) { }

        public override async Task<X509Certificate2> Handle(IntermediateCertificateRequest request, CancellationToken cancellationToken)
        {
            return await base.Handle(request, cancellationToken);
        }

        protected override async Task<X509Certificate2> GetNewCertificate(ChainedCertificateRequest request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("{request}: Request certificate from remote", request.GetType().Name);
            var newcert = await this.SendRemote(request, cancellationToken);
            return newcert;
        }

        public override async Task Handle(RenewIntermediateCertificateNotification notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask; // handled by NotificationForwarder
        }
    }
}