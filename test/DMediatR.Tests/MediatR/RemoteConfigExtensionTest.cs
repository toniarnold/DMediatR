using DMediatR.Tests.MediatR.LocalRemote;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace DMediatR.Tests
{
    public class RemoteConfigExtensionTest
    {
        [Test]
        public void WhenRemoteConfiguredTest()
        {
            // check  an inexistent class and the implicitly given name for RemoteHandler
            const string configRemote = /*lang=json,strict*/ """"
                {
                    "Remotes": {
                        "RemoteHandler": {
                            "Host": "localhost",
                            "Port": 8081,
                            "OldPort": 8082
                            }
                    }
                }
                """";
            var config = new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configRemote)))
               .Build();

            Assert.Multiple(() =>
            {
                Assert.That(config.SelectLocalRemote(typeof(RemoteHandler)), Is.True,
                    "RemoteHandler is configured, thus select");
                Assert.That(config.SelectLocalRemote(typeof(LocalHandler)), Is.False,
                    "Configured RemoteHandler disables LocalHandler");
            });
        }

        [Test]
        public void WhenRemoteNotConfiguredTest()
        {
            const string configRemote = "{}";
            var config = new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configRemote)))
               .Build();

            Assert.Multiple(() =>
            {
                Assert.That(config.SelectLocalRemote(typeof(RemoteHandler)), Is.False,
                    "RemoteHandler is not configured, thus ignore");
                Assert.That(config.SelectLocalRemote(typeof(LocalHandler)), Is.True,
                    "RemoteHandler is not configured for LocalHandler, thus select");
            });
        }
    }
}