using CertificateManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

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
                .AddLogging(builder => builder.AddConsole())
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

            // Throughout X509Chain validation
            var policy = new X509ChainPolicy();
            policy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            policy.RevocationMode = X509RevocationMode.NoCheck;
            policy.ApplicationPolicy.Add(OidLookup.ClientAuthentication);
            policy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            policy.ExtraStore.Add(rootCert);
            policy.ExtraStore.Add(interCert);
            policy.ExtraStore.Add(serverCert);
            policy.ExtraStore.Add(clientCert);

            using var chain = new X509Chain() { ChainPolicy = policy };
            var valid = chain.Build(clientCert);
            var errors = new List<X509ChainStatusFlags>();
            if (!valid)
            {
                foreach (var validationFailure in chain.ChainStatus)
                {
                    errors.Add(validationFailure.Status);
                }
            }
            Assert.That(valid, Is.True);
        }
    }
}