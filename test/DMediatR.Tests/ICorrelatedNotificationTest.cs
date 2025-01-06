using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMediatR.Tests
{
    public class ICorrelatedNotificationTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void AddServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection cs = new();
            _serviceProvider = cs.AddDMediatR(cfg)
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void CorrelationGuidSerializerdTest()
        {
            var serializer = _serviceProvider.GetRequiredService<ISerializer>();
            var bing = new Bing("msg");  // ICorrelatedNotification
            var serializationGuid = bing.CorrelationGuid;
            var bytes = serializer.Serialize(bing);
            var copy = serializer.Deserialize<Bing>(bytes);
            Assert.That(copy.CorrelationGuid, Is.EqualTo(serializationGuid));
        }
    }
}