using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace DMediatR

{
    /// <summary>
    /// One data transfer object class for any type.
    /// </summary>
    [DataContract]
    public class Dto
    {
        /// <summary>
        /// The type of the serialized object.
        /// </summary>
        [DataMember(Order = 1)]
        public Type Type { get; set; } = typeof(object);

        /// <summary>
        /// The binary serialized object.
        /// </summary>
        [DataMember(Order = 2)]
        public byte[] Bytes { get; set; } = [];
    }

    /// <summary>
    /// Code-first gRPC service for sending a MediatR IRequest to the remote IRequestHandler.
    /// </summary>
    [ServiceContract]
    public interface IDtoService
    {
        [OperationContract]
        Task<Dto> SendAsync(Dto request,
            CallContext context = default);

        [OperationContract]
        Task PublishAsync(Dto notification,
            CallContext context = default);
    }
}