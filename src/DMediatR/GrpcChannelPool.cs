using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace DMediatR
{
    /// <summary>
    /// Its internal implementation provides a cache for long-lived gRPC channels, one instance per address.
    /// </summary>
    public interface IGrpcChannelPool : IDisposable
    {
        GrpcChannel ForAddress(string address, HttpClientHandler handler);

        bool Remove(string address);
    }

    /// <summary>
    /// Implements a cache for long-lived gRPC channels, one instance per address.
    /// </summary>
    internal class GrpcChannelPool : IGrpcChannelPool
    {
        private readonly ConcurrentDictionary<string, GrpcChannel> _channelCache = new();

        public GrpcChannel ForAddress(string address, HttpClientHandler handler)
        {
            var channel = _channelCache.AddOrUpdate<HttpMessageHandler>(address,
                (address, handler) => // addValueFactory
                {
                    return GrpcChannel.ForAddress(address,
                         new GrpcChannelOptions { HttpHandler = handler });
                },
                (address, channel, handler) => // updateValueFactory
                {
                    return channel;
                },
                  handler
                 );
            return channel;
        }

        public bool Remove(string address)
        {
            var found = _channelCache.Remove(address, out GrpcChannel? channel);
            channel?.Dispose();
            return found;
        }

        public void Dispose()
        {
            foreach (var channel in _channelCache.Values)
            {
                try
                {
                    channel.ShutdownAsync().Wait();
                }
                catch { }
                finally
                {
                    channel.Dispose();
                }
            }
        }
    }
}