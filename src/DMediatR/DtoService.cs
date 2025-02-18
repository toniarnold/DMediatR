using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace DMediatR

{
    /// <summary>
    /// Dto consumer to be used in the gRPC server.
    /// </summary>
    public class DtoService : IDtoService
    {
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;
        private readonly IMemoryCache _cache;
        private readonly CertificateOptions _certOptions;
        private readonly GrpcOptions _grpcOptions;

        public DtoService(
            IMediator mediator,
            ISerializer serializer,
            IMemoryCache cache,
            IOptions<CertificateOptions> certOptions,
            IOptions<GrpcOptions> grpcOptions)
        {
            _mediator = mediator;
            _serializer = serializer;
            _cache = cache;
            _certOptions = certOptions.Value;
            _grpcOptions = grpcOptions.Value;
        }

        /// <summary>
        /// Send the deserialized request to the local handler.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<Dto> SendAsync(Dto requestDto, CallContext context = default)
        {
            // Ignore a RemotesGraphRequest when RemotesSvg is not configured by
            // returning the request unchanged without forwarding it to the
            // RemotesGraphHandler.
            if (requestDto.Type == typeof(RemotesGraphRequest) && !(_grpcOptions.RemotesSvg ?? true))
            {
                return requestDto;
            }
            else
            {
                var request = _serializer.Deserialize(requestDto.Type, requestDto.Bytes);
                var responseType = ((IBaseRequest)request).GetResponseType();
                if (responseType != null)
                {
                    var response = await _mediator.Send(request);
                    var responseDto = new Dto() { Type = responseType, Bytes = _serializer.Serialize(response!) };
                    return responseDto;
                }
                else
                {
                    await _mediator.Send(request);
                    return new Dto();   // ≙ null
                }
            }
        }

        /// <summary>
        /// Send the deserialized notification to the local handlers including
        /// NotificationForwarder. De-duplicates notifications received in
        /// multiple copies over different network paths. Also ignores
        /// certificate RenewNotification messages if RenewFirewallEnabled is
        /// true in CertificateOptions (default).
        /// </summary>
        /// <param name="notificationDto"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task PublishAsync(Dto notificationDto, CallContext context = default)
        {
            var notification = _serializer.Deserialize(notificationDto.Type, notificationDto.Bytes);
            if (notification is not ICorrelatedNotification) // from RemoteExtension.PublishRemote
            {
                throw new ArgumentException($"Expected ICorrelatedNotification, but got {notification.GetType()}");
            }
            if (!(notification is RenewNotification && _certOptions.RenewFirewallEnabled) &&
               (!_cache.HaveSeen(((ICorrelatedNotification)notification).CorrelationGuid, _grpcOptions.MaxLatency)))
            {
                await _mediator.Publish(notification);
            }
        }
    }
}