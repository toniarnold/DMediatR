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
            return await this.SendRemote(request, cancellationToken);
        }
    }
}