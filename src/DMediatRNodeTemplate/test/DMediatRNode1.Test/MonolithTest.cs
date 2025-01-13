using Microsoft.Extensions.DependencyInjection;

namespace DMediatRNode1.Test
{
    /// <summary>
    /// Use this unit test assembly together with DMediatRNode1 as a monolith
    /// without any configured remotes.
    /// </summary>
    public class MonolithTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SetUp.SetUpDMediatRServices();
        }

        [Test]
        public async Task RequestResponseTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var response = await mediator.Send(new Request1 { Message = "from NUnit" });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.EqualTo("Pong from NUnit")); // locally
        }

        [Test]
        public async Task BingTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new Bing("from NUnit"));
        }
    }
}