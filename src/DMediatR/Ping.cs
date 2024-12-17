namespace DMediatR
{
    /// <summary>
    /// Simple MediatR IRequest for diagnosis.
    /// </summary>
    public class Ping : SerializationCountMessage, IRequest<Pong>
    {
        public Ping()
        {
        }

        public Ping(string message)
        {
            Message = message;
        }
    }
}