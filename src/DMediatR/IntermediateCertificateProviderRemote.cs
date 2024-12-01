using CertificateManager;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("IntermediateCertifier")]
    internal class IntermediateCertificateProviderRemote : IntermediateCertificateProvider, IRemoteInternal
    {
        public IntermediateCertificateProviderRemote(
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel,
            ImportExportCertificate ioCert)
                : base(createCert, hostOptions, certOptions, remotesOptions, mediator, serializer, channel, ioCert) { }

        public override async Task<X509Certificate2> Handle(IntermediateCertificateRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}