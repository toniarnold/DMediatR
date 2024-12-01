using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;

namespace DMediatR.Tests.MediatR.LocalRemote
{
    public class ConfigureLocalRemoteTest
    {
        [Test]
        public async Task ConfigureLocal()
        {
            var config = new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes("{}")))
               .Build();
            var services = new ServiceCollection();
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
                cfg.TypeEvaluator = t => config.SelectLocalRemote(t);
            });
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var pong = await mediator.Send(new Ping { Message = "Ping" });
            Assert.That(pong.Message, Is.EqualTo("Ping local Pong"));
        }

        [Test]
        public async Task ConfigureRemote()
        {
            const string remoteConfig = /*lang=json,strict*/ """
                {
                    "Remotes": {
                        "RemoteHandler": {
                            "Host": "localhost",
                            "Port": 8081,
                            "OldPort": 8082
                            }
                    }
                }
                """;
            var config = new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(remoteConfig)))
               .Build();
            var services = new ServiceCollection();
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
                cfg.TypeEvaluator = t => config.SelectLocalRemote(t);
            });
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            var pong = await mediator.Send(new Ping { Message = "Ping" });
            Assert.That(pong.Message, Is.EqualTo("Ping remote Pong"));
        }

        private IConfiguration LocalConfig()
        {
            return new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes("{}")))
               .Build();
        }

        private IConfiguration RemoteConfig()
        {
            const string configWithRemotes =  /*lang=json,strict*/ """
                {
                    "Remotes": [
                        {
                            "name": "RemoteHandler",
                            "host": "localhost",
                            "port": "2001"
                        }
                    ]
                }
                """;
            var config = new ConfigurationBuilder()
               .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configWithRemotes)))
               .Build();
            return config;
        }
    }
}