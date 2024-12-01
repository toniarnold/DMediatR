using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests
{
    public class CertificateProviderTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void AddServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection cs = new();
            _serviceProvider = cs.AddDMediatR(cfg)
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void GenerateCertificateChain()
        {
            // ------- Root CA certificate -------
            var rootGen = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            var rootReq = new RootCertificateRequest();
            var rootCert = rootGen.Generate(rootReq);

            // ------- Intermediate certificate -------
            var interReq = new IntermediateCertificateRequest();
            var interGen = _serviceProvider.GetRequiredService<IntermediateCertificateProvider>();
            var interCert = interGen.Generate(interReq, rootCert);

            // ------- Server node certificate -------
            var serverReq = new ServerCertificateRequest();
            var serverGen = _serviceProvider.GetRequiredService<ServerCertificateProvider>();
            var serverCert = serverGen.Generate(serverReq, interCert);

            // ------- Client node certificate -------
            var clientReq = new ClientCertificateRequest();
            var clientGen = _serviceProvider.GetRequiredService<ClientCertificateProvider>();
            var clientCert = clientGen.Generate(clientReq, interCert);

            // Rudimentary smoke test assertions
            Assert.Multiple(() =>
            {
                Assert.That(rootCert.Subject, Is.EqualTo("CN=RootCertifier"));
                Assert.That(interCert.Subject, Is.EqualTo("CN=IntermediateCertifier"));
                Assert.That(serverCert.Subject, Is.EqualTo("CN=ServerCertifier"));
                Assert.That(clientCert.Subject, Is.EqualTo("CN=ClientCertifier"));
            });
        }
    }
}