using Grpc.AspNetCore.Server;
using Grpc.Net.Client;
using MessagePack;

namespace DMediatR
{
    /// <summary>
    /// Optional gRPC configuration Facade aligning client and server
    /// configuration with single values.
    /// </summary>
    public class GrpcOptions
    {
        public const string SectionName = "DMediatR:Grpc";

        /// <summary>
        /// GrpcServiceOptions.EnableDetailedErrors
        /// </summary>
        public bool? EnableDetailedErrors { get; set; }

        /// <summary>
        /// Aligns
        /// GrpcServiceOptions.MaxReceiveMessageSize
        /// GrpcServiceOptions.MaxSendMessageSize
        /// GrpcChannelOptions.MaxReceiveMessageSize
        /// GrpcChannelOptions.MaxSendMessageSize
        /// </summary>
        public int? MaxMessageSize { get; set; }

        /// <summary>
        /// Proxy for static CorrelatedNotification.MaxLatency
        /// </summary>

        public static TimeSpan? MaxLatency
        {
            get { return CorrelatedNotification.MaxLatency; }
            set { if (value != null) CorrelatedNotification.MaxLatency = (TimeSpan)value; }
        }

        /// <summary>
        /// Proxy for static MessagePack.MessagePackCompression. Ensure that all
        /// clients and servers are configured for compression or omit it on all
        /// nodes to avoid a MessagePackSerializationException. Enum values:
        /// None|Lz4Block|Lz4BlockArray
        /// </summary>

        public static MessagePackCompression? MessagePackCompression
        {
            get { return MessagePackSerializer.Typeless.DefaultOptions.Compression; }
            set
            {
                if (value == null || value == MessagePack.MessagePackCompression.None)
                {
                    MessagePackSerializer.Typeless.DefaultOptions = MessagePack.Resolvers.TypelessContractlessStandardResolver.Options;
                }
                else
                {
                    MessagePackSerializer.Typeless.DefaultOptions =
                        MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithCompression(
                            (MessagePackCompression)value);
                }
            }
        }

        /// <summary>
        /// AddCodeFirstGrpc exposes configuration as
        /// Action<GrpcServiceOptions>. Setting null for *MessageSize properties
        /// of GrpcServiceOptions is not exactly side-effect-free, thus only
        /// assign when not null.
        /// </summary>
        /// <param name="serverOptions"></param>
        internal void AssignOptions(GrpcServiceOptions serverOptions)
        {
            if (EnableDetailedErrors != null) serverOptions.EnableDetailedErrors = EnableDetailedErrors;
            if (MaxMessageSize != null)
            {
                serverOptions.MaxSendMessageSize = MaxMessageSize;
                serverOptions.MaxReceiveMessageSize = MaxMessageSize;
            }
        }

        /// <summary>
        /// GrpcChannel.ForAddress expects a GrpcChannelOptions object for
        /// configuration. Configure it if MaxMessageSize is set.
        /// </summary>
        /// <returns>Configured GrpcChannelOptions object</returns>
        internal GrpcChannelOptions GetGrpcChannelOptions()
        {
            var options = new GrpcChannelOptions();
            if (MaxMessageSize != null)
            {
                options.MaxSendMessageSize = MaxMessageSize;
                options.MaxReceiveMessageSize = MaxMessageSize;

                if (options.MaxRetryAttempts != null)
                {
                    options.MaxRetryBufferPerCallSize = MaxMessageSize;
                    options.MaxRetryBufferPerCallSize = options.MaxRetryAttempts * MaxMessageSize;
                }
            }
            return options;
        }
    }
}