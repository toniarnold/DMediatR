using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DMediatR
{
    internal class BingHandler : INotificationHandler<Bing>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _correlationGuidCache;
        private readonly ILogger<BingHandler> _logger;

        public BingHandler(IServiceProvider serviceProvider, IMemoryCache cache, ILogger<BingHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _correlationGuidCache = cache;
            _logger = logger;
        }

        // <binghandler>
        public async Task Handle(Bing notification, CancellationToken cancellationToken)
        {
            if (!_correlationGuidCache.HaveSeen(this.GetType(), notification.CorrelationGuid))
            {
                var msg = $"{notification.Message}";
                _logger.LogInformation("Handling {msg}", msg);
                await Task.CompletedTask;
            }
        }

        // </binghandler>
    }
}