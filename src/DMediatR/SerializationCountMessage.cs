using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DMediatR
{
    /// <summary>
    /// Base class for tracing the number of hops a DMediatR message (IRequest or INotification) has taken.
    /// Its Count property gets incremented whenever it gets serialized.
    /// </summary>
    public abstract class SerializationCountMessage
    {
        /// <summary>
        /// The Message payload.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Number of times the message has been serialized.
        /// </summary>
        public uint Count { get; set; } = 0;

        /// <summary>
        /// Amend a trace diagnosis like "Bing N hops {Message} via host" to the message.
        /// </summary>
        /// <param name="bing"></param>
        /// <returns></returns>
        internal static void AddTraceToMessage(IServiceProvider serviceProvider, SerializationCountMessage messageObject)
        {
            var className = messageObject.GetType().Name;

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
            var via = (messageObject.Count > 0 && host != "") ? $" via {host}" : "";

            string newMessage = "";
            switch (messageObject.Count)
            {
                case 0:
                    newMessage = $"{className} {messageObject.Message}";
                    break;

                case 1:
                    newMessage = $"{Regex.Replace($"{messageObject.Message}", $@"(^{className})", $"$1 {messageObject.Count} hops")}{via}";
                    break;

                default: // uint >1, already contains "hops", thus only replace the number and append another "via"
                    newMessage = $"{Regex.Replace($"{messageObject.Message}", $@"(^{className})( \d+)( hops .*$)", $"$1 {messageObject.Count}$3")}{via}";
                    break;
            }
            messageObject.Message = newMessage;
        }
    }
}