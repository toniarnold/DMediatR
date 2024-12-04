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
    /// Composite object injecting all required dependencies for SendRemote().
    /// </summary>
    public class Remote
    {
        private readonly CertificateOptions _certOptions;
        private readonly RemotesOptions _remotes;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IGrpcChannelPool _grpcChannelProvider;

        public Remote(
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

        internal CertificateOptions CertificateOptions => _certOptions;
        internal RemotesOptions Remotes => _remotes;
        internal IMediator Mediator => _mediator;
        internal ISerializer Serializer => _serializer;
        internal IGrpcChannelPool ChannelPool => _grpcChannelProvider;
    }
}