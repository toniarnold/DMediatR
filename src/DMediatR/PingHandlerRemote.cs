using Microsoft.Extensions.Logging;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;

namespace DMediatR
{
    [Remote("Ping")]
    internal class PingHandlerRemote : PingHandler, IRemote
    {
        private readonly Remote _remote;

        public PingHandlerRemote(Remote remote, IServiceProvider serviceProvider, ILogger<PingHandlerRemote> logger) : base(serviceProvider, logger)
        {
            _remote = remote;
        }

        public Remote Remote => _remote;

        public override async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            var pong = await this.SendRemote(request, cancellationToken);

            // This handler is run on the node which has called the remote, adjust the hops here
            // for the Pong message created in the base PingHandler.
            if (pong.Count > 0)
            {
                var msg = Regex.Replace($"{pong.Message}", @"(^Pong)( \d+)( hops .*$)", $"$1 {pong.Count}$3");
                pong.Message = msg;
            }
            _logger.LogInformation("{message}", pong.Message);

            return pong;
        }
    }
}