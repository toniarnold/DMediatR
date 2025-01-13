using Microsoft.Extensions.DependencyInjection;

namespace DMediatRNode1.Test
{
    /// <summary>
    /// The unit test assembly is the client, the one remote is DMediatRNode1 in
    /// the configuration Red. Request1 is configured to be handler remote on
    /// red, its encapsulated Ping is handled locally on Red and thus adds no
    /// hops trace information.
    /// </summary>
    public class RedTest
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
        public async Task RequestResponseTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(new Request1 { Message = "from NUnit" });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.EqualTo("Pong from NUnit"));
        }

        [Test]
        public async Task BingTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new Bing("from NUnit"));
        }
    }
}