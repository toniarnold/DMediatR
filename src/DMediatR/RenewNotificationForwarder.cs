using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    internal class RenewNotificationForwarder : INotificationHandler<ICorrelatedNotification>, IRemote
    {
        private readonly Remote _remote;
        private readonly IMemoryCache _correlationGuidCache;
        private readonly HostOptions _host;
        private readonly CertificateOptions _certOptions;
        private readonly RemotesOptions _remotes;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IGrpcChannelPool _grpcChannelProvider;

        public RenewNotificationForwarder(Remote remote,
            IMemoryCache cache,
            IOptions<HostOptions> hostOptions,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel)
        {
            _remote = remote;
            _correlationGuidCache = cache;
            _host = hostOptions.Value;
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
        }

        public Remote Remote => _remote;
        public IMediator Mediator => _mediator;
        public ISerializer Serializer => _serializer;
        public IGrpcChannelPool ChannelPool => _grpcChannelProvider;
        public CertificateOptions Options => _certOptions;
        public RemotesOptions Remotes => _remotes;

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