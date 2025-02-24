using CertificateManager;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        /// Send a MediatR IRequest to a configured remote node and receive a TResponse.
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
        /// Send a MediatR IRequest to an explicitly given remote node and receive a TResponse.
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="provider"></param>
        /// <param name="toHost"></param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<TResponse> SendRemote<TResponse>(this IRemote provider, HostOptions toHost, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            return await RemoteInternalExtension.InternalSendRemote(provider, toHost, request, cancellationToken);
        }

        /// <summary>
        ///  Publish the notification to all distinct connected remote nodes.
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
            var toHost = Remote(provider);
            return await RemoteInternalExtension.InternalSendRemote(provider, toHost, request, cancellationToken);
        }

        internal static async Task<TResponse> InternalSendRemote<TResponse>(this IRemote provider, HostOptions toHost, IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            var hasLocked = (request as ILock)?.HasLocked ?? [];
            var requestDto = Dto(provider, request);
            Dto responseDto;
            var address = toHost.Address;
            var oldAddress = toHost.OldAddress;
            var options = provider.Remote.GrpcOptions.GrpcChannelOptions;
            bool renew = request is CertificateRequest && ((CertificateRequest)request).Renew;
            try
            {
                provider.Remote.Logger.LogDebug("Send {request} to {address}",
                    request.GetType().Name, address);
                var handler = await provider.GetHttpClientHandler(renew, GrpcPort.UseDefault, hasLocked, cancellationToken);
                var channel = provider.Remote.ChannelPool.ForAddress(address, options, handler);
                var client = channel.CreateGrpcService<IDtoService>();
                responseDto = await client.SendAsync(requestDto);
            }
            catch (RpcException defaultPortEx)
            {
                if (defaultPortEx.StatusCode != StatusCode.Unknown)
                {
                    throw new GrpcException(defaultPortEx);  // The connection itself was successful.
                }
                // Enforce reconnect
                provider.Remote.ChannelPool.Remove(address);
                provider.Remote.ChannelPool.Remove(oldAddress);
                try
                {
                    provider.Remote.Logger.LogDebug("Renew client certificate for {address} on {oldAddress}", address, oldAddress);

                    // Request a new client certificate through the old certificate chain.
                    var oldHandler = await provider.GetHttpClientHandler(true, GrpcPort.UseRenew, hasLocked, cancellationToken);
                    await RenewClientCertificate(provider, oldAddress, oldHandler, options, cancellationToken);

                    // Reconnect and repeat with the obtained new client certificate.
                    var handler = await provider.GetHttpClientHandler(false, GrpcPort.UseDefault, hasLocked, cancellationToken);
                    var channel = provider.Remote.ChannelPool.ForAddress(address, options, oldHandler);
                    var client = channel.CreateGrpcService<IDtoService>();
                    responseDto = await client.SendAsync(requestDto);
                }
                catch (RpcException oldPortEx)
                {
                    throw new AggregateException($"Client certificate renewal failure for {address} on {oldAddress}",
                        [defaultPortEx, oldPortEx]);
                }
            }
            var response = provider.Remote.Serializer.Deserialize<TResponse>(responseDto.Bytes);
            return response;
        }

        internal static async Task InternalPublishRemote(this IRemote provider, ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            if (provider.Remote.Remotes.Count > 0)
            {
                var tasks = new List<Task>();
                var hasLocked = (notification as ILock)?.HasLocked ?? [];
                var notificationDto = Dto(provider, notification);
                var remotes = (provider.Remote.Remotes.Values).DistinctBy(r => r.Address);
                foreach (var remoteHost in remotes)
                {
                    provider.Remote.Logger.LogDebug("Forwarding {msg} to {host}:{port}",
                        notification.GetType().Name, remoteHost.Host, remoteHost.Port);
                    var t = PublishNotificationDtoRemote(provider, remoteHost, notificationDto, hasLocked, cancellationToken);
                    tasks.Add(t);
                }
                try
                {
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    if (notification is RenewServerCertificateNotification)
                    {
                        // Assume that the server certificates have been renewed, thus reconnect.
                        foreach (var remoteHost in provider.Remote.Remotes.Values)
                        {
                            provider.Remote.ChannelPool.Remove(remoteHost.Address);
                            provider.Remote.ChannelPool.Remove(remoteHost.OldAddress);
                        }
                    }
                }
            }
            else
            {
                await Task.CompletedTask;
            }
        }

        private static async Task PublishNotificationDtoRemote(IRemote provider, HostOptions remoteHost, Dto notificationDto, HashSet<SemaphoreSlim> hasLocked, CancellationToken cancellationToken)
        {
            var address = remoteHost.Address;
            var oldAddress = remoteHost.OldAddress;
            var options = provider.Remote.GrpcOptions.GrpcChannelOptions;
            try
            {
                var handler = await provider.GetHttpClientHandler(false, GrpcPort.UseDefault, hasLocked, cancellationToken);
                var channel = provider.Remote.ChannelPool.ForAddress(address, options, handler);
                var client = channel.CreateGrpcService<IDtoService>();
                await client.PublishAsync(notificationDto);
            }
            catch (RpcException defaultPortEx)
            {
                if (defaultPortEx.StatusCode != StatusCode.Unknown)
                {
                    throw new GrpcException(defaultPortEx);  // The connection itself was successful.
                }
                // Enforce reconnect
                provider.Remote.ChannelPool.Remove(address);
                provider.Remote.ChannelPool.Remove(oldAddress);
                try
                {
                    provider.Remote.Logger.LogDebug("Renew client certificate for {address} on {oldAddress}", address, oldAddress);

                    // Request a new client certificate through the old certificate chain.
                    var oldHandler = await provider.GetHttpClientHandler(true, GrpcPort.UseRenew, hasLocked, cancellationToken);
                    await RenewClientCertificate(provider, oldAddress, oldHandler, options, cancellationToken);

                    // Reconnect and repeat with the obtained new client certificate.
                    var handler = await provider.GetHttpClientHandler(false, GrpcPort.UseDefault, hasLocked, cancellationToken);
                    var channel = provider.Remote.ChannelPool.ForAddress(address, options, handler);
                    var client = channel.CreateGrpcService<IDtoService>();
                    await client.PublishAsync(notificationDto); ;
                }
                catch (RpcException oldPortEx)
                {
                    throw new AggregateException($"Client certificate renewal failure for {address} on {oldAddress}",
                        [defaultPortEx, oldPortEx]);
                }
            }
        }

        private static Dto Dto(this IRemote provider, object obj)
        {
            return new Dto() { Type = obj.GetType(), Bytes = provider.Remote.Serializer.Serialize(obj) };
        }

        private static HostOptions Remote(this IRemote provider)
        {
            var remoteName = RemoteAttribute.Name(provider.GetType());
            if (remoteName == null)
            {
                throw new Exception(
                    "Called SendRemote() from a class without matching [Remote(\"<Name>\")] attribute");
            }
            return provider.Remote.Remotes[remoteName];
        }

        /// <summary>
        /// Get a HttpClientHandler with client certificate TLS authentication.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="renew"></param>
        /// <param name="usePort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<HttpClientHandler> GetHttpClientHandler(this IRemote provider,
            bool renew, GrpcPort usePort, HashSet<SemaphoreSlim> hasLocked, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                    usePort == GrpcPort.UseRenew ? IsServerCertificateValidOld : IsServerCertificateValid
            };
            X509Certificate2? clientCertificate;

            clientCertificate = await provider.Remote.Mediator.Send(new ClientCertificateRequest() { Renew = renew, HasLocked = hasLocked }, cancellationToken);
            GrpcServer.LoadIntermediateCertificates(provider.Remote.ServiceProvider);
            handler.ClientCertificates.Add(clientCertificate!);
            return handler;
        }

        private static async Task RenewClientCertificate(IRemote provider,
            string oldAddress, HttpClientHandler handler, GrpcChannelOptions options, CancellationToken cancellationToken)
        {
            var oldChannel = provider.Remote.ChannelPool.ForAddress(oldAddress, options, handler);
            var dtoService = oldChannel.CreateGrpcService<IDtoService>();

            // 1. Obtain the new client certificate and the validation intermediate certificate
            var newClientCertDto = await dtoService.SendAsync(Dto(provider, new ClientCertificateRequest() { Renew = true }));
            var newClientCert = provider.Remote.Serializer.Deserialize<X509Certificate2>(newClientCertDto.Bytes);
            var newInterCertDto = await dtoService.SendAsync(Dto(provider, new IntermediateCertificateRequest() { Renew = true }));
            var newInterCert = provider.Remote.Serializer.Deserialize<X509Certificate2>(newInterCertDto.Bytes);

            // 2. Save the client certificate with private key, from the validating certificate only the public part
            var clientProvider = provider.Remote.ServiceProvider.GetRequiredService<ClientCertificateProvider>();
            var interProvider = provider.Remote.ServiceProvider.GetRequiredService<IntermediateCertificateProvider>();
            await clientProvider.Save(newClientCert, newInterCert, cancellationToken);
            await interProvider.SaveCrt(newInterCert, cancellationToken);
        }

        private static bool IsServerCertificateValid(HttpRequestMessage requestMessage, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return IsServerCertificateValid(false, requestMessage, cert, chain, sslErrors);
        }

        private static bool IsServerCertificateValidOld(HttpRequestMessage requestMessage, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return IsServerCertificateValid(true, requestMessage, cert, chain, sslErrors);
        }

        private static bool IsServerCertificateValid(bool old, HttpRequestMessage requestMessage, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            // Connecting to a  linux-arm64 server fails if (sslErrors != SslPolicyErrors.None)
            bool valid = false;
            var policy = new X509ChainPolicy
            {
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority |
                    (old ? X509VerificationFlags.IgnoreCtlNotTimeValid : 0)
            };
            policy.ApplicationPolicy.Add(OidLookup.ServerAuthentication);
            policy.ExtraStore.Add(old ? GrpcServer.IntermediateCrtOld! : GrpcServer.IntermediateCrt!);
            policy.ExtraStore.Add(cert!);
            if (chain != null)
            {
                chain.ChainPolicy = policy;
                valid = chain.Build(cert!);

                if (!valid)
                {
                    var status = from s in chain.ChainStatus select $"{s.Status}: {s.StatusInformation}";
                    string failures = System.String.Join("\n          ", status);
                    GrpcServer.Logger!.LogWarning("Server certificate custom validation failure {status}\n{failures}", status, failures);
                }
            }
            return valid;
        }
    }
}