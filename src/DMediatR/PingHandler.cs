using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    [Local("Ping")]
    internal class PingHandler : IRequestHandler<Ping, Pong>
    {
        private readonly IServiceProvider _serviceProvider;

        public PingHandler(IServiceProvider serviceProvider)

        {
            _serviceProvider = serviceProvider;
        }

        public virtual async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
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
            await Task.CompletedTask;
            return new Pong { Message = $"{request.Message}{from}" };
        }
    }
}