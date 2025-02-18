using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR.Tests
{
    public class RemotesGraphTest
    {
        private const string FILENAME_SVG = "remotes.RemotesGraphTest.svg";

        private ServiceProvider _serviceProvider = default!;

        [OneTimeSetUp]
        public void AddServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            sc.AddDMediatR(cfg)
              .AddLogging(builder => builder.AddConsole());

            // Replace the read config with this one for generating the SVG:
            var host = new HostOptions() { Host = "source", Port = 8081 };
            var remotes = new RemotesOptions()
            {
                { "IntermediateCertificateRequest", new HostOptions() { Host = "target", Port = 8082 } },
                { "Request2", new HostOptions() { Host = "targetnode.example.com", Port = 8083 } }
            };
            sc.Replace(new ServiceDescriptor(typeof(IOptions<HostOptions>), Options.Create(host)));
            sc.Replace(new ServiceDescriptor(typeof(IOptions<RemotesOptions>), Options.Create(remotes)));

            _serviceProvider = sc.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public async Task CreateGraph()
        {
            var request = new RemotesGraphRequest();
            request.Recursive = false;
            var handler = _serviceProvider.GetRequiredService<RemotesGraphHandler>();
            var resultRequest = await handler.Handle(request, CancellationToken.None);
            var renderer = _serviceProvider.GetRequiredService<IRemotesGraph>();
            var svg = renderer.GetSvg(resultRequest);
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "output");
            File.WriteAllText(Path.Combine(path, FILENAME_SVG), svg);
        }
    }
}