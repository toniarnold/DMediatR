using Microsoft.Extensions.DependencyInjection;

namespace DMediatR
{
    /// <summary>
    /// Utility to create a valid initial X509 certificate chain offline to be distributed to all gRPC nodes.
    /// </summary>
    public class Certificates
    {
        private readonly IServiceProvider _serviceProvider;

        public Certificates(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Generate or renew the certificate chain by directly using the local services.
        /// </summary>
        public void SetUpInitialChain()
        {
            Task.Run(() => SetUpInitialChainAsync(CancellationToken.None)).Wait();
        }

        /// <summary>
        /// Generate or renew the certificate chain by directly using the local services.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SetUpInitialChainAsync(CancellationToken cancellationToken)
        {
            // Create them in the order required by chain dependency by directly
            // calling the local providers. Save it explicitly a second time to also
            // generate the -old certificates.
            var rootProvider = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            var rootCert = await rootProvider.Handle(new RootCertificateRequest(), cancellationToken);
            await rootProvider.Save(rootCert, cancellationToken);

            var interProvider = _serviceProvider.GetRequiredService<IntermediateCertificateProvider>();
            var interCert = await interProvider.Handle(new IntermediateCertificateRequest(), cancellationToken);
            await interProvider.Save(interCert, rootCert, cancellationToken);

            var serverProvider = _serviceProvider.GetRequiredService<ServerCertificateProvider>();
            var serverCert = await serverProvider.Handle(new ServerCertificateRequest(), cancellationToken);
            await serverProvider.Save(serverCert, interCert, cancellationToken);

            var clientProvider = _serviceProvider.GetRequiredService<ClientCertificateProvider>();
            var clientCert = await clientProvider.Handle(new ClientCertificateRequest(), cancellationToken);
            await clientProvider.Save(clientCert, interCert, cancellationToken);
        }
    }
}