﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace DMediatR.Tests
{
    public class MemoryCacheGuidExtensionTest
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
        public void HaveSeenTest()
        {
            var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            Assert.That(cache.HaveSeen(guid1), Is.False);
            Assert.That(cache.HaveSeen(guid2), Is.False);
            // Solely querying HaveSeen adds it to the cache:
            Assert.That(cache.HaveSeen(guid1), Is.True);
            Assert.That(cache.HaveSeen(guid2), Is.True);
        }
    }
}