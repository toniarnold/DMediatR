using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMediatR.Tests
{
    public class MemoryCacheGuidExtensionTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void AddServices()

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
        public void HaveSeenTest()
        {
            var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var latency = new TimeSpan(1, 0, 0, 0, 0);
            Assert.That(cache.HaveSeen(guid1, latency), Is.False);
            Assert.That(cache.HaveSeen(guid2, latency), Is.False);

            // Solely querying HaveSeen adds it to the cache:
            Assert.That(cache.HaveSeen(guid1, latency), Is.True);
            Assert.That(cache.HaveSeen(guid2, latency), Is.True);
        }
    }
}