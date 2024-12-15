using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMediatR.Tests
{
    internal class NotificationForwarderTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void AddServices()

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
        public void AddTraceToMessageTest()
        {
            var bing = new Bing("-msg-");
            NotificationForwarder.AddTraceToMessage(_serviceProvider, bing);
            Assert.That(bing.Message, Is.EqualTo("Bing -msg-"));

            bing.Count++;
            NotificationForwarder.AddTraceToMessage(_serviceProvider, bing);
            Assert.That(bing.Message, Is.EqualTo("Bing 1 hops -msg- via localhost:8081"));

            bing.Count++;
            NotificationForwarder.AddTraceToMessage(_serviceProvider, bing);
            Assert.That(bing.Message, Is.EqualTo("Bing 2 hops -msg- via localhost:8081 via localhost:8081"));
        }
    }
}