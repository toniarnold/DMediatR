using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace DMediatR
{
    internal interface IGrpcChannelPool : IDisposable
    {
        GrpcChannel ForAddress(string address, HttpClientHandler handler);

        void Remove(string address);
    }

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

        public void Remove(string address)
        {
            _channelCache.Remove(address, out GrpcChannel? channel);
            channel?.Dispose();
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