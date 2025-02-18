namespace DMediatR
{
    public class RemotesGraphRequest : IRequest<RemotesGraphRequest>, ICorrelatedMessage
    {
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();

        // Can't use theMicrosoft.Msagl.Drawing as accumulator directly as it is
        // not serializable with MessagePack (although it basically seems
        // intended to be according to the used [NonSerialized] attribute),
        // therefore use declarative shallow objects:
        public readonly record struct Node(string Id);
        public readonly record struct Edge(string SourceId, string Label, string TargetId);

        public List<Node> Nodes = [];
        public List<Edge> Edges = [];

        /// <summary>
        /// Allows disabling depth-first traversal of target nodes in tests.
        /// </summary>
        internal bool Recursive = true;
    }
}