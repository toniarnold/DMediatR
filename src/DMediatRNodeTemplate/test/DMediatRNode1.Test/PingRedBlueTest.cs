using Microsoft.Extensions.DependencyInjection;

namespace DMediatRNode1.Test
{
    /// <summary>
    /// Same as PingRedTest, but with two nodes, but node Red transitively
    /// delegates Ping to Blue, which in in turn will add another hop to the
    /// trace.
    /// </summary>
    public class PingRedBlueTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SetUp.SetUpDMediatRServices();
            SetUp.SetUpInitialCertificates();
            DeployCertificates();
            SetUp.SetUpDMediatRServices("RemotePingRed");
            StartServers(); // or ./start Red Blue
            SetUp.AssertServersStarted();
        }

        private void StartServers()
        {
            SetUp.StartServer("Blue", 18003, 18004);
            SetUp.StartServer("RedPingBlue", 18005, 18006);
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
            SetUp.DeployCertificate("DMediatR-Intermediate.pfx", "Red");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "Red");
            SetUp.DeployCertificate("DMediatR-Client.pfx", "Red");
        }

        [Test]
        public async Task RequestResponseTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(new Request1 { Message = "from NUnit" });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.EqualTo("Pong 4 hops from NUnit via RedPingBlue via Blue via RedPingBlue via localhost:8081"));
        }

        [Test]
        public async Task BingTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new Bing("from NUnit"));
        }
    }
}