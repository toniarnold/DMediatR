using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    internal class NotificationForwarder : INotificationHandler<ICorrelatedNotification>, IRemote
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Remote _remote;
        private readonly IMemoryCache _correlationGuidCache;

        public NotificationForwarder(IServiceProvider serviceProvider, Remote remote, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _remote = remote;
            _correlationGuidCache = cache;
        }

        public Remote Remote => _remote;

        /// <summary>
        /// Distribute the notification to all connected remote nodes.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Handle(ICorrelatedNotification notification, CancellationToken cancellationToken)
        {
            if (!_correlationGuidCache.HaveSeen(notification.CorrelationGuid))
            {
                if (notification is Bing)
                {
                    // Append diagnosis "via host N hops" to the message.
                    var bing = (Bing)notification;
                    var host = "";
                    var env = Environment.GetEnvironmentVariables();
                    if (env.Contains("ASPNETCORE_ENVIRONMENT"))
                    {
                        host = (string)env["ASPNETCORE_ENVIRONMENT"]!;  // dotnet run --project
                    }
                    else
                    {
                        var hostOptions = _serviceProvider.GetService<IOptions<HostOptions>>();
                        if (hostOptions != null)
                        {
                            host = $"{hostOptions.Value.Host}:{hostOptions.Value.Port}";
                        }
                    }
                    var via = (bing.Count > 0 && host != "") ? $" via {host}" : "";
                    var hops = bing.Count > 0 ? $" {bing.Count} hops" : "";
                    var msg = $"{bing.Message}{via}{hops}";
                    bing.Message = msg;
                    notification = bing;
                }
                await this.PublishRemote(notification, cancellationToken);
            }
        }
    }
}