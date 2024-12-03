using Microsoft.Extensions.Options;

namespace DMediatR
{
    [Remote("Ping")]
    internal class PingHandlerRemote : PingHandler, IRemoteInternal
    {
        protected readonly CertificateOptions _certOptions;
        protected readonly RemotesOptions _remotes;
        protected readonly IMediator _mediator;
        protected readonly ISerializer _serializer;
        protected readonly IGrpcChannelPool _grpcChannelProvider;

        public PingHandlerRemote(

            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel)
        {
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
        }

        public IGrpcChannelPool ChannelPool => _grpcChannelProvider;

        public CertificateOptions Options => _certOptions;
        public ISerializer Serializer => _serializer;

        public RemotesOptions Remotes => _remotes;
        public IMediator Mediator => _mediator;

        public override async Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}