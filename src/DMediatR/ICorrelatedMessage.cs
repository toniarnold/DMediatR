namespace DMediatR
{
    /// <summary>
    /// Multiple DMediatR nodes can have cyclic dependencies or there might be
    /// indirect diamonds in the configured dependency graph. In such cases a
    /// single node receives and would forward the same message in multiple
    /// duplicate copies. To handle them only once as in a monolith correlate
    /// these remote messages with a Guid.
    /// </summary>
    public interface ICorrelatedMessage
    {
        /// <summary>
        /// To be implemented as
        /// public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
        /// </summary>
        Guid CorrelationGuid { get; init; }
    }
}