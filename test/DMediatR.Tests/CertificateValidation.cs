using CertificateManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR.Tests
{
    [Category("File")]
    public class CertificateValidation
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            _serviceProvider = sc.AddDMediatR(cfg)
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();

            var certs = _serviceProvider.GetRequiredService<Certificates>();
            certs.SetUpInitialChain();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public async Task ValidateClientCertificate()
        {
            var clientCertProvider = _serviceProvider.GetRequiredService<ClientCertificateProvider>();
            (var loaded, var clientCert) = await clientCertProvider.TryLoad(CancellationToken.None);
            Assert.That(loaded, Is.True);
            var intermediateCertProvider = _serviceProvider.GetRequiredService<IntermediateCertificateProvider>();
            (loaded, var interCert) = await intermediateCertProvider.TryLoad(CancellationToken.None);
            Assert.That(loaded, Is.True);
            //var rootCertProvider = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            //(loaded, var rootCert) = await rootCertProvider.TryLoad(CancellationToken.None);
            //Assert.That(loaded, Is.True);

            var policy = new X509ChainPolicy();
            policy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
            policy.RevocationMode = X509RevocationMode.NoCheck;
            policy.ApplicationPolicy.Add(OidLookup.ClientAuthentication);
            policy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            //policy.ExtraStore.Add(rootCert!);
            policy.ExtraStore.Add(interCert!);
            policy.ExtraStore.Add(clientCert!);

            using var chain = new X509Chain() { ChainPolicy = policy };
            var valid = chain.Build(clientCert!);
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