using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    internal class BingHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BingHandler> _logger;

        public BingHandler(IServiceProvider serviceProvider, ILogger<BingHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Handle(Bing notification, CancellationToken cancellationToken)
        {
            var host = "";
            var env = Environment.GetEnvironmentVariables();
            if (env.Contains("ASPNETCORE_ENVIRONMENT"))
            {
                host = (string)env["ASPNETCORE_ENVIRONMENT"]!;  // dotnet run --project
            }
            else
            {
                var hostOptions = _serviceProvider.GetService<IOptions<HostOptions>>();
                if (hostOptions != null)
                {
                    host = $"{hostOptions.Value.Host}:{hostOptions.Value.Port}";
                }
            }
            var from = (host != "") ? $" from {host}" : "";
            var hops = notification.Count > 0 ? $"{notification.Count} hops " : "";
            var msg = $"Bing {hops}{notification.Message}{from}";
            _logger.LogInformation(msg);
            await Task.CompletedTask;
        }
    }
}