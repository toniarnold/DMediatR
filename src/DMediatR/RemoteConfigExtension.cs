using Microsoft.Extensions.Configuration;

namespace DMediatR
{
    internal static class RemoteConfigExtension
    {
        public static bool SelectLocalRemote(this IConfiguration cfg, Type t)
        {
            return (RemoteAttribute.Name(t) == null && LocalAttribute.RemoteName(t) == null)
                || (RemoteAttribute.Name(t) != null && IsRemoteConfigured(cfg, RemoteAttribute.Name(t)))
                || (LocalAttribute.RemoteName(t) != null && !IsRemoteConfigured(cfg, LocalAttribute.RemoteName(t)));
        }

        private static bool IsRemoteConfigured(this IConfiguration cfg, string? name)
        {
            var configuredRemotes = cfg.GetSection(RemotesOptions.SectionName).Get<RemotesOptions>();
            return configuredRemotes != null && configuredRemotes.ContainsKey(name!);
        }
    }
}