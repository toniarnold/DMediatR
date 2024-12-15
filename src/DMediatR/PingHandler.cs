using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    [Local("Ping")]
    internal class PingHandler : IRequestHandler<Ping, Pong>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PingHandler> _logger;

        public PingHandler(IServiceProvider serviceProvider, ILogger<PingHandler> logger)

        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public virtual async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var hops = request.Count > 0 ? $" {request.Count} hops " : "";
            _logger.LogInformation("Ping{hops}{message}", hops, request.Message);

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
            var via = (host != "") ? $" via {host}" : "";
            var pongMsg = $"Pong{hops}{request.Message}{via}"; // hops of the Ping
            return new Pong { Message = pongMsg, Count = request.Count };
        }
    }
}