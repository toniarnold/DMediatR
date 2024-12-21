using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc.Client;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    /// <summary>
    /// IRemote extension methods for connecting to a Dto gRPC service from a MediatR handler.
    /// </summary>
    public static class RemoteExtension
    {
        /// <summary>
        /// Send a MediatR IRequest to a remote node and receive a TResponse.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="provider">The class providing a MediatR handler.</param>
        /// <param name="request">The MediatR IRequest.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The MediatR TResponse.</returns>
        public static async Task<TResponse> SendRemote<TResponse>(this IRemote provider, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            return await RemoteInternalExtension.InternalSendRemote(provider, request, cancellationToken);
        }

        /// <summary>
        /// Publish a MediatR notification to a remote node.
        /// </summary>
        /// <param name="provider">The class providing a MediatR handler</param>
        /// <param name="notification">The MediatR ICorrelatedNotification.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task PublishRemote(this IRemote provider, ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            await RemoteInternalExtension.InternalPublishRemote(provider, notification, cancellationToken);
        }
    }

    internal static class RemoteInternalExtension
    {
        internal static async Task<TResponse> InternalSendRemote<TResponse>(this IRemote provider, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            var isClientCertificateRequest = request.GetType() == typeof(ClientCertificateRequest);
            var handler = await provider.GetHttpClientHandler(isClientCertificateRequest, cancellationToken);
            var requestDto = Dto(provider, request);
            Dto responseDto;
            var address = $"https://{Remote(provider).Host}:{Remote(provider).Port}";
            var oldAddress = $"https://{Remote(provider).Host}:{Remote(provider).OldPort}";
            try
            {
                var channel = provider.Remote.ChannelPool.ForAddress(address, handler);
                var client = channel.CreateGrpcService<IDtoService>();
                responseDto = await client.SendAsync(requestDto);
            }
            catch (HttpRequestException defaultPortEx)
            {
                provider.Remote.ChannelPool.Remove(address); // reconnect
                try
                {
                    // 1. Request a new client certificate through the old certificate chain.
                    var oldChannel = provider.Remote.ChannelPool.ForAddress(oldAddress, handler);
                    var certClient = oldChannel.CreateGrpcService<IDtoService>();
                    await certClient.SendAsync(Dto(provider, new ClientCertificateRequest()));

                    // 2. Reconnect with the obtained new client certificate.
                    var channel = provider.Remote.ChannelPool.ForAddress(address, handler);
                    var client = channel.CreateGrpcService<IDtoService>();
                    responseDto = await client.SendAsync(requestDto);
                }
                catch (Exception oldPortEx)
                {
                    provider.Remote.ChannelPool.Remove(oldAddress);
                    throw new AggregateException(defaultPortEx, oldPortEx);
                }
            }
            var response = provider.Remote.Serializer.Deserialize<TResponse>(responseDto.Bytes);
            return response;
        }

        public static async Task InternalPublishRemote(this IRemote provider, ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            if (provider.Remote.Remotes.Count > 0)
            {
                var tasks = new List<Task>();
                var notificationDto = Dto(provider, notification);
                var handler = await provider.GetHttpClientHandler(false, cancellationToken);
                foreach (var remote in provider.Remote.Remotes.Values)
                {
                    var t = PublishNotificationDtoRemote(provider, remote, notificationDto, handler);
                    tasks.Add(t);
                }
                await Task.WhenAll(tasks);
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        private static async Task PublishNotificationDtoRemote(IRemote provider, HostOptions remote, Dto notificationDto, HttpClientHandler handler)
        {
            var address = $"https://{remote.Host}:{remote.Port}";
            var oldAddress = $"https://{remote.Host}:{remote.OldPort}";
            try
            {
                var channel = provider.Remote.ChannelPool.ForAddress(address, handler);
                var client = channel.CreateGrpcService<IDtoService>();
                await client.PublishAsync(notificationDto);
            }
            catch (HttpRequestException defaultPortEx)
            {
                provider.Remote.ChannelPool.Remove(address); // reconnect
                try
                {
                    // 1. Request a new client certificate through the old certificate chain.
                    var oldChannel = provider.Remote.ChannelPool.ForAddress(oldAddress, handler);
                    var certClient = oldChannel.CreateGrpcService<IDtoService>();
                    await certClient.SendAsync(Dto(provider, new ClientCertificateRequest()));

                    // 2. Reconnect with the obtained new client certificate.
                    var channel = provider.Remote.ChannelPool.ForAddress(address, handler);
                    var client = channel.CreateGrpcService<IDtoService>();
                    await client.PublishAsync(notificationDto); ;
                }
                catch (Exception oldPortEx)
                {
                    throw new AggregateException(defaultPortEx, oldPortEx);
                }
            }
        }

        private static Dto Dto(this IRemote provider, object obj)
        {
            return new Dto() { Type = obj.GetType(), Bytes = provider.Remote.Serializer.Serialize(obj) };
        }

        private static HostOptions Remote(this IRemote provider)
        {
            var remoteName = RemoteAttribute.Name(provider.GetType())!;
            return provider.Remote.Remotes[remoteName];
        }

        /// <summary>
        /// Get a HttpClientHandler with client certificate SSL
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="handleClientCertificateRequest">True if the remote request is a ClientCertificateRequest itself</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<HttpClientHandler> GetHttpClientHandler(this IRemote provider,
            bool handleClientCertificateRequest, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidation
            };
            X509Certificate2? clientCertificate;
            if (handleClientCertificateRequest)
            {
                // If the remote request is a ClientCertificateRequest itself, avoid an endless loop.
                // The connection has to happen with a local certificate anyway, thus get it directly.
                var clientCertProvider = provider.Remote.ServiceProvider.GetRequiredService<ClientCertificateProvider>();
                (var loaded, clientCertificate) = await clientCertProvider.TryLoad(CancellationToken.None);
                if (!loaded)
                {
                    throw new Exception($"Client certificate {clientCertProvider.FileName} not found");
                }
            }
            else
            {
                clientCertificate = await provider.Remote.Mediator.Send(new ClientCertificateRequest(), cancellationToken);
            }
            handler.ClientCertificates.Add(clientCertificate!);
            return handler;
        }

        private static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return sslErrors == SslPolicyErrors.None;
        }
    }
}