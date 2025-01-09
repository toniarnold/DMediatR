using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DMediatRNode1.Test
{
    public class TwoNodesTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SetUp.SetUpDMediatRServices();
            SetUp.SetUpInitialCertificates();
            DeployCertificates();
            SetUp.SetUpDMediatRServices("RemoteBlueRed");
            StartServers(); // or ./start Red Blue
            SetUp.AssertServersStarted();
        }

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }

        private void DeployCertificates()
        {
            SetUp.DeployCertificate("DMediatR-Server.pfx", "Blue");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "Blue");

            SetUp.DeployCertificate("DMediatR-Server.pfx", "Red");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "Red");
            SetUp.DeployCertificate("DMediatR-Client.pfx", "Red");
        }

        private void StartServers()
        {
            SetUp.StartServer("Blue", 18003, 18004);
            SetUp.StartServer("Red", 18005, 18006);
        }

        [Test]
        public async Task PingPongTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var pongFromRemote = await mediator.Send(new Ping("from NUnit"));
            Assert.That(pongFromRemote, Is.Not.Null);
            Assert.That(pongFromRemote.Count, Is.EqualTo(2));
            Assert.That(pongFromRemote.Message, Is.EqualTo("Pong 2 hops from NUnit via Red via localhost:8081"));
        }

        [Test]
        public async Task BingTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new Bing("from NUnit"));
        }
    }
}