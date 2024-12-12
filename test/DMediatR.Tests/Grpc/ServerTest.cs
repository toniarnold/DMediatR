using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests.Grpc
{
    [Category("Integration")]
    public class ServerTest
    {
        [OneTimeSetUp]
        public void SetUpInitialCertificatesStartServer()
        {
            SetUp.SetUpDMediatRServices("Monolith");
            SetUp.SetUpInitialCertificates();

            SetUp.SetUpDMediatRServices("RemoteServerCert");
            SetUp.StartServer("Monolith", 18001, 18002);
            Assert.That(SetUp.ServerProcesses, Has.Count.EqualTo(1));
            var process = SetUp.ServerProcesses[0];
            Assert.That(process.HasExited, Is.False, "Process was not started");
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            SetUp.StopAllServers();
        }

        [Test]
        public async Task GrpcChannelPoolTest()
        {
            var mediator = SetUp.ServiceProvider!.GetRequiredService<IMediator>();
            await mediator.Send(new ServerCertificateRequest());
            await mediator.Send(new ServerCertificateRequest());
        }

        [Test]
        public async Task GetRemoteServerCert()
        {
            var mediator = SetUp.ServiceProvider!.GetRequiredService<IMediator>();
            var certFromRemote = await mediator.Send(new ServerCertificateRequest());
            Assert.That(certFromRemote, Is.Not.Null);
            Assert.That(certFromRemote.Subject, Is.EqualTo("CN=ServerCertifier"));
        }

        [Test]
        public async Task GetRemoteClientCert()
        {
            var mediator = SetUp.ServiceProvider!.GetRequiredService<IMediator>();
            var certFromRemote = await mediator.Send(new ClientCertificateRequest());
            Assert.That(certFromRemote, Is.Not.Null);
            Assert.That(certFromRemote.Subject, Is.EqualTo("CN=ClientCertifier"));
        }

        [Test]
        public async Task ForceRenewRemoteServerCert()
        {
            var mediator = SetUp.ServiceProvider!.GetRequiredService<IMediator>();
            // Requesting it twice yields the same certificate from the filesysem.
            var oldCertFromRemote = await mediator.Send(new ServerCertificateRequest());
            var oldCertDouble = await mediator.Send(new ServerCertificateRequest());
            Assert.That(oldCertFromRemote.Thumbprint, Is.EqualTo(oldCertDouble.Thumbprint));

            await mediator.Publish(new RenewServerCertificateNotification());
            // After explicit renewal the gRPC server is restarted and yelds a new certificate.
            var newCertFromRemote = await mediator.Send(new ServerCertificateRequest());
            Assert.That(newCertFromRemote.Thumbprint, Is.Not.EqualTo(oldCertDouble.Thumbprint));
        }
    }
}