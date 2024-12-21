using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("ClientCertifier")]
    internal class ClientCertificateProviderRemote : ClientCertificateProvider, IRemote
    {
        public ClientCertificateProviderRemote(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<ClientCertificateProvider> logger)
                : base(remote, createCert, hostOptions, ioCert, logger) { }

        public override async Task<X509Certificate2> Handle(ClientCertificateRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}