using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

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
                    notification = AddTraceToMessage(_serviceProvider, (Bing)notification);
                }
                await this.PublishRemote(notification, cancellationToken);
            }
        }

        /// <summary>
        /// Add trace diagnosis "Bing N hops {Message} via host" to the message.
        /// </summary>
        /// <param name="bing"></param>
        /// <returns></returns>
        internal static Bing AddTraceToMessage(IServiceProvider serviceProvider, Bing bing)
        {
            //
            var host = "";
            var env = Environment.GetEnvironmentVariables();
            if (env.Contains("ASPNETCORE_ENVIRONMENT"))
            {
                host = (string)env["ASPNETCORE_ENVIRONMENT"]!;  // dotnet run --project
            }
            else
            {
                var hostOptions = serviceProvider.GetService<IOptions<HostOptions>>();
                if (hostOptions != null)
                {
                    host = $"{hostOptions.Value.Host}:{hostOptions.Value.Port}";
                }
            }
            var via = (bing.Count > 0 && host != "") ? $" via {host}" : "";
            string msg = "";
            switch (bing.Count)
            {
                case 0:
                    msg = $"Bing {bing.Message}";
                    break;

                case 1:
                    msg = $"{Regex.Replace($"{bing.Message}", @"(^Bing)", $"$1 {bing.Count} hops")}{via}";
                    break;

                default: // uint >1, already contains "hops", thus only replace the number and append another "via"
                    msg = $"{Regex.Replace($"{bing.Message}", @"(^Bing)( \d+)( hops .*$)", $"$1 {bing.Count}$3")}{via}";
                    break;
            }
            bing.Message = msg;
            return bing;
        }
    }
}