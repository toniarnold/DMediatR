using Microsoft.Extensions.Caching.Memory;

namespace DMediatR
{
    internal static class MemoryCacheGuidExtension
    {
        /// <summary>
        /// True if the guid is already in the cache, else add it to the cache for the next time.
        /// Defaults to 1 day if maxLatency is not configured.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static bool HaveSeen(this IMemoryCache cache, Guid guid, TimeSpan? maxLatency)
        {
            var cached = true;
            cache.GetOrCreate(
                guid,
                entry => // Create
                {
                    cached = false;
                    entry.AbsoluteExpirationRelativeToNow = maxLatency ?? new(1, 0, 0, 0);
                    return new object();
                });
            return cached;
        }
    }
}