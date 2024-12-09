using Microsoft.Extensions.Caching.Memory;

namespace DMediatR
{
    internal class RenewNotificationForwarder : INotificationHandler<ICorrelatedNotification>, IRemote
    {
        private readonly Remote _remote;
        private readonly IMemoryCache _correlationGuidCache;

        public RenewNotificationForwarder(Remote remote, IMemoryCache cache)
        {
            _remote = remote;
            _correlationGuidCache = cache;
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
            if (!_correlationGuidCache.HaveSeen(notification.CorrelationGuid))
            {
                await this.PublishRemote(notification, cancellationToken);
            }
        }
    }
}