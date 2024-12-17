using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DMediatR
{
    [Local("Ping")]
    internal class PingHandler : IRequestHandler<Ping, Pong>
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger<PingHandler> _logger;

        public PingHandler(IServiceProvider serviceProvider, ILogger<PingHandler> logger)

        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public virtual async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            // This handler is run locally on the remote node, thus log here:
            SerializationCountMessage.AddTraceToMessage(_serviceProvider, request); // case 1, add 1 hops
            _logger.LogInformation("{message}", request.Message);

            var pongMsg = Regex.Replace($"{request.Message}", $@"(^Ping )(.*$)", @"Pong $2");
            var pong = new Pong { Message = pongMsg, Count = request.Count }; // Count of the Ping
            return pong;
        }
    }
}