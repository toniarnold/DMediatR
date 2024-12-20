﻿using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests
{
    public class PingSerializerTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection cs = new();
            _serviceProvider = cs.AddDMediatR(cfg)
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