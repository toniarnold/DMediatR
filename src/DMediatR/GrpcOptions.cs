using Grpc.AspNetCore.Server;
using Grpc.Net.Client;
using MessagePack;
using System.IO.Compression;

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
        /// Duration the CorrelationGuid of a ICorrelatedNotification should
        /// stay in the cache. Required to prevent it from indefinitely growing.
        /// Afterwards, received duplicate copies of a notifications can no more
        /// be correlated and thus ignored. Defaults to 1 day, but can be
        /// adjusted to specific workloads here.
        /// </summary>
        public TimeSpan? MaxLatency { get; set; }

        /// <summary>
        /// Header grpc-accept-encoding compression algorithm natively
        /// supported: gzip
        /// </summary>
        public string? ResponseCompressionAlgorithm { get; set; }

        /// <summary>
        /// The compress level used to compress messages sent from the server.
        /// </summary>
        public CompressionLevel? ResponseCompressionLevel { get; set; }

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
        /// GrpcChannel.ForAddress expects a GrpcChannelOptions object for
        /// configuration. Configure it if MaxMessageSize is set.
        /// </summary>
        /// <returns>Configured GrpcChannelOptions object</returns>
        internal GrpcChannelOptions GrpcChannelOptions
        {
            get
            {
                var options = new GrpcChannelOptions();
                if (MaxMessageSize != null)
                {
                    options.MaxSendMessageSize = MaxMessageSize;
                    options.MaxReceiveMessageSize = MaxMessageSize;
                }
                return options;
            }
        }

        /// <summary>
        /// AddCodeFirstGrpc exposes the configuration as
        /// Action<GrpcServiceOptions>. Setting null for *MessageSize properties
        /// of GrpcServiceOptions also sets _maxSendMessageSizeConfigured, thus
        /// only assign them when not null.
        /// </summary>
        /// <param name="serverOptions"></param>
        internal void AssignOptions(GrpcServiceOptions serverOptions)
        {
            if (MaxMessageSize != null)
            {
                serverOptions.MaxSendMessageSize = MaxMessageSize;
                serverOptions.MaxReceiveMessageSize = MaxMessageSize;
            }
            serverOptions.EnableDetailedErrors = EnableDetailedErrors;
            serverOptions.ResponseCompressionAlgorithm = ResponseCompressionAlgorithm;
            serverOptions.ResponseCompressionLevel = ResponseCompressionLevel;
        }
    }
}