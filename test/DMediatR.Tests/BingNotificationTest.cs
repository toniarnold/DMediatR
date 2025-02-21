using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMediatR.Tests
{
    public class BingNotificationTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            _serviceProvider = sc.AddDMediatR(cfg)
                .AddLogging(builder => builder.AddNUnit())
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public async Task PublishBing()
        {
            var bing = new Bing("from NUnit");
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(bing);
            // no assertions here, just used for debugging with breakpoints in handlers
        }
    }
}