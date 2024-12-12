using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    internal class BingHandler : INotificationHandler<Bing>
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
            var msg = $"{notification.Message}";
            _logger.LogInformation(msg);
            await Task.CompletedTask;
        }
    }
}