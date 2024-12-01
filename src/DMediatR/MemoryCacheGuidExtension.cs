using Microsoft.Extensions.Caching.Memory;

namespace DMediatR
{
    internal static class MemoryCacheGuidExtension
    {
        public static bool HaveSeen(this IMemoryCache cache, Guid guid)
        {
            var cached = cache.GetOrCreate(
                guid,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = RenewNotification.MaxLatency;
                    return new object();
                });
            return cached != null;
        }
    }
}