using Microsoft.Extensions.Logging;

namespace DMediatR
{
    internal class NotificationForwarder : INotificationHandler<ICorrelatedNotification>, IRemote
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Remote _remote;
        private readonly ILogger<NotificationForwarder> _logger;

        public NotificationForwarder(IServiceProvider serviceProvider, Remote remote, ILogger<NotificationForwarder> logger)
        {
            _serviceProvider = serviceProvider;
            _remote = remote;
            _logger = logger;
        }

        public Remote Remote => _remote;

        /// <summary>
        /// Publish the notification to all distinct connected remote nodes.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Handling/Forwarding {msg}", notification.GetType().Name);
            if (notification is SerializationCountMessage sc)
            {
                SerializationCountMessage.AddTraceToMessage(_serviceProvider, sc);
                if (notification is Bing)
                {
                    _logger.LogInformation("Handling/Forwarding Bing {msg}", sc.Message);
                }
            }
            await this.PublishRemote(notification, cancellationToken);
        }
    }
}