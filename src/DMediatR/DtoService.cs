using ProtoBuf.Grpc;

namespace DMediatR

{
    /// <summary>
    /// Dto consumer to be used in the gRPC server.
    /// </summary>
    public class DtoService : IDtoService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly ISerializer _serializer;

        public DtoService(
            IServiceProvider serviceProvider,
            IMediator mediator,
            ISerializer serializer)
        {
            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _serializer = serializer;
        }

        /// <summary>
        /// Send the deserialized request to the local handler.
        /// </summary>
        /// <param name="requestDto"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<Dto> SendAsync(Dto requestDto, CallContext context = default)
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

        /// <summary>
        /// Send the deserialized notification to the local handlers and to all configured remotws.
        /// </summary>
        /// <param name="notificationDto"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task PublishAsync(Dto notificationDto, CallContext context = default)
        {
            var notification = _serializer.Deserialize(notificationDto.Type, notificationDto.Bytes);
            await _mediator.Publish(notification);
        }
    }
}