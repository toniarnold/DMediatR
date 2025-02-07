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
        private readonly IServiceProvider _serviceProvider;
        private readonly CertificateOptions _certOptions;
        private readonly RemotesOptions _remotes;
        private readonly GrpcOptions _grpcOptions;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IGrpcChannelPool _grpcChannelProvider;
        private readonly ILogger<Remote> _logger;

        public Remote(
            IServiceProvider serviceProvider,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IOptions<GrpcOptions> grpcOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel,
            ILogger<Remote> logger)
        {
            _serviceProvider = serviceProvider;
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _grpcOptions = grpcOptions.Value;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
            _logger = logger;
        }

        internal CertificateOptions CertificateOptions => _certOptions;
        internal GrpcOptions GrpcOptions => _grpcOptions;
        internal RemotesOptions Remotes => _remotes;
        internal IServiceProvider ServiceProvider => _serviceProvider;
        internal IMediator Mediator => _mediator;
        internal ISerializer Serializer => _serializer;
        internal IGrpcChannelPool ChannelPool => _grpcChannelProvider;
        internal ILogger<Remote> Logger => _logger;
    }
}