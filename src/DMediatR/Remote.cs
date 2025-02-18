using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    /// <summary>
    /// Inherit from IRemote and inject Remote to add the SendRemote extension.
    /// </summary>
    public interface IRemote
    {
        Remote Remote { get; }
    }

    /// <summary>
    /// Public composite object injecting all internal dependencies required for
    /// RemoteExtension.SendRemote().
    /// </summary>
    public class Remote
    {
        private readonly CertificateOptions _certOptions;
        private readonly RemotesOptions _remotes;
        private readonly GrpcOptions _grpcOptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IGrpcChannelPool _grpcChannelProvider;
        private readonly IRemotesGraph _remotesGraph;
        private readonly ILogger<Remote> _logger;

        public Remote(

            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IOptions<GrpcOptions> grpcOptions,
            IServiceProvider serviceProvider,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel,
            IRemotesGraph remotesGraph,
            ILogger<Remote> logger)
        {
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _grpcOptions = grpcOptions.Value;

            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
            _remotesGraph = remotesGraph;
            _logger = logger;
        }

        // The active options are public:

        public CertificateOptions CertificateOptions => _certOptions;
        public GrpcOptions GrpcOptions => _grpcOptions;
        public RemotesOptions Remotes => _remotes;

        // But the implementation details are internal:

        internal IServiceProvider ServiceProvider => _serviceProvider;
        internal IMediator Mediator => _mediator;
        internal ISerializer Serializer => _serializer;
        internal IGrpcChannelPool ChannelPool => _grpcChannelProvider;
        internal IRemotesGraph RemotesGraph => _remotesGraph;
        internal ILogger<Remote> Logger => _logger;
    }
}