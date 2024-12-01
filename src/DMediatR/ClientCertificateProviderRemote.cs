using CertificateManager;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("ClientCertifier")]
    internal class ClientCertificateProviderRemote : ClientCertificateProvider, IRemoteInternal
    {
        public ClientCertificateProviderRemote(
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel,
            ImportExportCertificate ioCert)
                : base(createCert, hostOptions, certOptions, remotesOptions, mediator, serializer, channel, ioCert) { }

        public override async Task<X509Certificate2> Handle(ClientCertificateRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}