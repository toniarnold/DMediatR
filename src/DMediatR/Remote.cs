using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

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
        private readonly IServiceProvider _serviceProvider;
        private readonly CertificateOptions _certOptions;
        private readonly RemotesOptions _remotes;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IGrpcChannelPool _grpcChannelProvider;

        public Remote(
            IServiceProvider serviceProvider,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel)
        {
            _serviceProvider = serviceProvider;
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
        }

        internal IServiceProvider ServiceProvider => _serviceProvider;
        internal CertificateOptions CertificateOptions => _certOptions;
        internal RemotesOptions Remotes => _remotes;
        internal IMediator Mediator => _mediator;
        internal ISerializer Serializer => _serializer;
        internal IGrpcChannelPool ChannelPool => _grpcChannelProvider;
    }
}