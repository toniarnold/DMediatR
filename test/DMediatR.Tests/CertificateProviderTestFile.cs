using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests
{
    /// <summary>
    /// CertificateProvider integration tests using the file system
    /// </summary>
    [Category("File")]
    public class CertificateProviderTestFile
    {
        private ServiceProvider _serviceProvider;

        private CertificateOptions Options =>
            _serviceProvider.GetRequiredService<IConfiguration>()
                .GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;

        [OneTimeSetUp]
        public void AddServicesDeleteTestFiles()

        {
            var cfg = Configuration.Get();
            ServiceCollection cs = new();
            _serviceProvider = cs.AddDMediatR(cfg)
                .BuildServiceProvider();

            DeleteTestFiles();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [TearDown]
        public void DeleteTestFiles()
        {
            string[] files = Directory.GetFiles(Options.FilePath!, $"{Options.FilenamePrefix}*");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        // Smoke test for the public certificate chain initialization method
        [Test]
        public void CertificatesSetUpInitialChainTest()
        {
            var certs = _serviceProvider.GetRequiredService<Certificates>();
            certs.SetUpInitialChain();
        }

        [Test]
        public async Task SaveTest()
        {
            var provider = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(provider.FileName), Is.False);
                Assert.That(File.Exists(provider.FileNameOld), Is.False);
            });

            await provider.Save(Array.Empty<byte>(), CancellationToken.None);
            Assert.Multiple(() =>   // only the saved file exists
            {
                Assert.That(File.Exists(provider.FileName), Is.True);
                Assert.That(File.Exists(provider.FileNameOld), Is.False);
            });

            await provider.Save(Array.Empty<byte>(), CancellationToken.None);
            Assert.Multiple(() =>   // above file was moved
            {
                Assert.That(File.Exists(provider.FileName), Is.True);
                Assert.That(File.Exists(provider.FileNameOld), Is.True);
            });
        }

        [Test]
        public async Task RequestNewRootCertificateTest()
        {
            var request = new RootCertificateRequest();
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var newCert = await mediator.Send(request);

            var provider = _serviceProvider.GetRequiredService<RootCertificateProvider>();
            Assert.Multiple(() =>
            {
                Assert.That(newCert, Is.Not.Null);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-RootCertifier.pfx")),
                    Is.True);
            });
            // Smoke Test: a second requests loads the same certificate just saved
            var loadedCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(loadedCert.Subject, Is.EqualTo(newCert.Subject));
                Assert.That(loadedCert.Thumbprint, Is.EqualTo(newCert.Thumbprint));
            });
        }

        [Test]
        public async Task RequestNewIntermediateCertificateTest()
        {
            var request = new IntermediateCertificateRequest();
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var newCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(newCert, Is.Not.Null);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-RootCertifier.pfx")),
                    Is.True);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-IntermediateCertifier.pfx")),
                    Is.True);
            });
            // Smoke Test: a second requests loads the same certificate just saved
            var loadedCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(loadedCert.Subject, Is.EqualTo(newCert.Subject));
                Assert.That(loadedCert.Thumbprint, Is.EqualTo(newCert.Thumbprint));
            });
        }

        [Test]
        public async Task RequestNewServerCertificateTest()
        {
            var request = new ServerCertificateRequest();
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var newCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(newCert, Is.Not.Null);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-RootCertifier.pfx")),
                    Is.True);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-IntermediateCertifier.pfx")),
                    Is.True);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-ServerCertifier.pfx")),
                    Is.True);
            });
            // Smoke Test: a second requests loads the same certificate just saved
            var loadedCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(loadedCert.Subject, Is.EqualTo(newCert.Subject));
                Assert.That(loadedCert.Thumbprint, Is.EqualTo(newCert.Thumbprint));
            });
        }

        [Test]
        public async Task RequestNewClientCertificateTest()
        {
            var request = new ClientCertificateRequest();
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var newCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(newCert, Is.Not.Null);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-RootCertifier.pfx")),
                    Is.True);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-IntermediateCertifier.pfx")),
                    Is.True);
                Assert.That(File.Exists(
                    Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-ClientCertifier.pfx")),
                    Is.True);
            });
            // Smoke Test: a second requests loads the same certificate just saved
            var loadedCert = await mediator.Send(request);
            Assert.Multiple(() =>
            {
                Assert.That(loadedCert.Subject, Is.EqualTo(newCert.Subject));
                Assert.That(loadedCert.Thumbprint, Is.EqualTo(newCert.Thumbprint));
            });
        }
    }
}