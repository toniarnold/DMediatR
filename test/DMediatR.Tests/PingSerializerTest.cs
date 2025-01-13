using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMediatR.Tests
{
    public class PingSerializerTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            _serviceProvider = sc.AddDMediatR(cfg)
                .AddLogging(builder => builder.AddConsole())
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void SerializeMulipleTimesCount()
        {
            var serializer = _serviceProvider.GetRequiredService<ISerializer>();
            Ping ping = new();
            for (int i = 0; i < 10; i++)
            {
                var bytes = serializer.Serialize(ping);
                ping = serializer.Deserialize<Ping>(bytes);
            }
            Assert.That(ping.Count, Is.EqualTo(10), $" with {serializer.GetType().Name}");
        }
    }
}