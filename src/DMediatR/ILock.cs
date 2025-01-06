namespace DMediatR
{
    /// <summary>
    /// Requests resp. notifications can recursively issue the same requests
    /// resp. notifications again. Attempting to lock a second time when the
    /// handler already holds a lock on that semaphore would cause a deadlock,
    /// therefore remember the locks already held for that message chain in the
    /// HasLocked set.
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// To be implemented as
        /// public HashSet&lt;SemaphoreSlim&gt; HasLocked { get; set; } = new();
        /// </summary>>
        HashSet<SemaphoreSlim>? HasLocked { get; set; }
    }

    public static class LockExtension
    {
        /// <summary>
        /// Lock the given semaphore only once in a call chain to avoid
        /// deadlocks. It is the responsibility of the issuer of a message to
        /// relay the HasLocked HashSet of the ILockingMessage down to
        /// potentially recursive successive messages.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="semaphore">SemaphoreSlim(1,1) object of the handler
        /// class.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if a lock was acquired before that must be released
        /// afterwards by the caller.</returns>
        public static async Task<bool> Lock(this ILock message, SemaphoreSlim semaphore, CancellationToken cancellationToken)
        {
            if (message.HasLocked.Add(semaphore)) // acquire the first lock
            {
                await semaphore.WaitAsync(cancellationToken);
                return true;
            }
            else // don't await the already locked semaphore
            {
                return false;
            }
        }
    }
}