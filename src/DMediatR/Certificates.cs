using Microsoft.Extensions.DependencyInjection;

namespace DMediatR
{
    public class Certificates
    {
        private readonly IServiceProvider _serviceProvider;

        public Certificates(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Generate or renew the certificate chain by directly using the local services
        /// </summary>
        public void SetUpInitialChain()
        {
            Task.Run(() => SetUpInitialChainAsync()).Wait();
        }

        /// <summary>
        /// Generate or renew the certificate chain by directly using the local services
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task SetUpInitialChainAsync(CancellationToken cancellationToken = default)
        {
            // Create them in the order required by chain dependency. Otherwise creating a certificate
            // might possibly try to get it from a configured remote node not reachable without client certificate.
            var root = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            await root.Handle(new RootCertificateRequest(), cancellationToken);
            var intermediate = _serviceProvider.GetRequiredService<IntermediateCertificateProvider>();
            await intermediate.Handle(new IntermediateCertificateRequest(), cancellationToken);
            var server = _serviceProvider.GetRequiredService<ServerCertificateProvider>();
            await server.Handle(new ServerCertificateRequest(), cancellationToken);
            var client = _serviceProvider.GetRequiredService<ClientCertificateProvider>();
            await client.Handle(new ClientCertificateRequest(), cancellationToken);
        }
    }
}