namespace DMediatR
{
    /// <summary>
    /// Simple MediatR IRequest for diagnosis.
    /// </summary>
    public class Ping : SerializationCountMessage, IRequest<Pong>
    {
        public string? Message { get; set; }

        public Ping()
        {
        }

        public Ping(string message)
        {
            Message = message;
        }
    }
}