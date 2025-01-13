using Microsoft.Extensions.Logging;

namespace DMediatR
{
    internal class BingHandler : INotificationHandler<Bing>, IRemote
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Remote _remote;
        private readonly ILogger<BingHandler> _logger;

        public Remote Remote => _remote;

        public BingHandler(IServiceProvider serviceProvider, Remote remote, ILogger<BingHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _remote = remote;
            _logger = logger;
        }

        public async Task Handle(Bing notification, CancellationToken cancellationToken)
        {
            var msg = $"{notification.Message}";
            _logger.LogInformation("Handling {msg}", msg);
            await Task.CompletedTask;
        }
    }
}