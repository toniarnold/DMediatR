using Microsoft.Extensions.Caching.Memory;

namespace DMediatR
{
    internal static class MemoryCacheGuidExtension
    {
        /// <summary>
        /// True if the guid is already in the cache, else add it to the cache for the next time.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static bool HaveSeen(this IMemoryCache cache, Type handlerClass, Guid guid)
        {
            var cached = true;
            cache.GetOrCreate(
                (handlerClass, guid),
                entry => // Create
                {
                    cached = false;
                    entry.AbsoluteExpirationRelativeToNow = CorrelatedNotification.MaxLatency;
                    return new object();
                });
            return cached;
        }
    }
}