﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMediatR.Tests
{
    public class BingNotificationTest
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
        public async Task PublishBing()
        {
            var bing = new Bing("from NUnit");
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(bing);
            // no assertions here, just used for debugging with breakpoints in handlers
        }
    }
}