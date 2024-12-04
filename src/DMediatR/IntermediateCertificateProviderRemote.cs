using CertificateManager;
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
            ImportExportCertificate ioCert)
                : base(remote, createCert, hostOptions, ioCert) { }

        public override async Task<X509Certificate2> Handle(IntermediateCertificateRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}