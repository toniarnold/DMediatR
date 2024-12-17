using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DMediatR
{
    internal class NotificationForwarder : INotificationHandler<ICorrelatedNotification>, IRemote
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Remote _remote;
        private readonly IMemoryCache _correlationGuidCache;
        private readonly ILogger<NotificationForwarder> _logger;

        public NotificationForwarder(IServiceProvider serviceProvider, Remote remote, IMemoryCache cache, ILogger<NotificationForwarder> logger)
        {
            _serviceProvider = serviceProvider;
            _remote = remote;
            _correlationGuidCache = cache;
            _logger = logger;
        }

        public Remote Remote => _remote;

        /// <summary>
        /// Distribute the notification to all connected remote nodes.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            if (!_correlationGuidCache.HaveSeen(this.GetType(), notification.CorrelationGuid))
            {
                if (notification is SerializationCountMessage)
                {
                    SerializationCountMessage.AddTraceToMessage(_serviceProvider, (SerializationCountMessage)notification);
                    if (notification is Bing)
                    {
                        _logger.LogInformation("Forwarding {msg}", ((SerializationCountMessage)notification).Message);
                    }
                }

                await this.PublishRemote(notification, cancellationToken);
            }
        }
    }
}