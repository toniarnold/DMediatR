namespace DMediatR
{
    /// <summary>
    /// Simple MediatR IRequest for diagnosis.
    /// </summary>
    public class Ping : SerializationCountMessage, IRequest<Pong>
    {
        /// <summary>
        /// Payload for performance testing.
        /// </summary>
        public byte[] Payload { get; set; } = [];

        public Ping()
        {
        }

        public Ping(string message)
        {
            Message = message;
        }
    }
}