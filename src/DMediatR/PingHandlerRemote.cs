using Microsoft.Extensions.Logging;

namespace DMediatR
{
    [Remote("Ping")]
    internal class PingHandlerRemote : PingHandler, IRemote
    {
        private readonly Remote _remote;

        public Remote Remote => _remote;

        public PingHandlerRemote(Remote remote, IServiceProvider serviceProvider, ILogger<PingHandlerRemote> logger) : base(serviceProvider, logger)
        {
            _remote = remote;
        }

        public override async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            SerializationCountMessage.AddTraceToMessage(_serviceProvider, request); // case 0, add Ping prefix
            var pong = await this.SendRemote(request, cancellationToken);

            // This handler is run on the node which has called the remote, adjust the hops here
            // for the Pong message created in the base PingHandler on the remote.
            SerializationCountMessage.AddTraceToMessage(_serviceProvider, pong); // case 2, 2 hops
            _logger.LogInformation("{message}", pong.Message);
            return pong;
        }
    }
}