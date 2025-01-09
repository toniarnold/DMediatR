using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DMediatRNode1.Test
{
    public class OneNodeTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SetUp.SetUpDMediatRServices();
            SetUp.SetUpInitialCertificates();
            DeployCertificates();
            SetUp.SetUpDMediatRServices("RemoteRed");
            SetUp.StartServer("Red", 18005, 18006); // or ./start Red
            SetUp.AssertServersStarted();
        }

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }

        private void DeployCertificates()
        {
            SetUp.DeployCertificate("DMediatR-Server.pfx", "Red");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "Red");
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
    }
}