namespace DMediatR
{
    /// <summary>
    /// Multiple DMediatR nodes can have cyclic dependencies or there might be
    /// indirect diamonds in the configured dependency graph. In such cases a
    /// single node receives and would forward the same INotification in multiple
    /// duplicate copies. To handle them only once as in a monolith correlate
    /// these remote Notifications with a Guid.
    /// </summary>
    public interface ICorrelatedNotification : INotification
    {
        /// <summary>
        /// To be implemented as
        /// public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
        /// </summary>
        Guid CorrelationGuid { get; init; }
    }

    /// <summary>
    /// Static configuration of ICorrelatedNotification for extreme workload patterns.
    /// </summary>
    public static class CorrelatedNotification
    {
        /// <summary>
        /// Duration the CorrelationGuid should stay in the cache. Required to
        /// prevent it from indefinitely growing. Afterwards, received duplicate
        /// copies of a notifications can no more be correlated and thus
        /// ignored. Defaults to 1 day, but can be adjusted to specific
        /// workloads here.
        /// </summary>
        public static TimeSpan MaxLatency { get; set; } = new(1, 0, 0, 0);
    }
}