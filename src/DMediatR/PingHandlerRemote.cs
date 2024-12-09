using Microsoft.Extensions.Logging;

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
            return await this.SendRemote(request, cancellationToken);
        }
    }
}