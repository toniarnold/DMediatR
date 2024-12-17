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

            // <startserver>
            SetUp.StartServer("Monolith", 18001, 18002);
            Assert.That(SetUp.ServerProcesses[0].HasExited, Is.False, "Process was not started");
            // </startserver>
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
        public async Task PingPongTest()
        {
            var mediator = SetUp.ServiceProvider!.GetRequiredService<IMediator>();
            var pongFromRemote = await mediator.Send(new Ping("from NUnit"));
            Assert.That(pongFromRemote, Is.Not.Null);
            Assert.That(pongFromRemote.Count, Is.EqualTo(2));
            Assert.That(pongFromRemote.Message, Is.EqualTo("Pong 2 hops from NUnit via Monolith via localhost:8081"));
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
            // After explicit renewal the gRPC server is restarted and yields a new certificate.
            SetUp.WaitForServerPort(18001);
            var newCertFromRemote = await mediator.Send(new ServerCertificateRequest());
            Assert.That(newCertFromRemote.Thumbprint, Is.Not.EqualTo(oldCertDouble.Thumbprint));
        }
    }
}