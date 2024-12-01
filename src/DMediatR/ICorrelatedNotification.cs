namespace DMediatR
{
    /// <summary>
    /// Multiple DMediatR nodes can have cyclic dependencies or there might be indirect diamonds in the configured dependency graph.
    /// In such cases a single node receives and would forward the same Notification in multiple duplicate copies.
    /// To handle them only once as in a monolith correlate these remote Notifications with a Guid.
    /// </summary>
    public interface ICorrelatedNotification : INotification
    {
        /// <summary>
        /// To be implemented as public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
        /// </summary>
        Guid CorrelationGuid { get; init; }
    }
}