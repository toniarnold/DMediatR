namespace DMediatR
{
    /// <summary>
    /// Base class for tracing the number of hops a DMediatR message (IRequest or INotification) has taken.
    /// Its Count property gets incremented whenever it gets serialized.
    /// </summary>
    public class SerializationCountMessage
    {
        /// <summary>
        /// Number of times the message has been serialized.
        /// </summary>
        public int Count { get; set; } = 0;
    }
}