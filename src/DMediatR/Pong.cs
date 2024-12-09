namespace DMediatR
{
    /// <summary>
    /// Response to a Ping request. Echoes the Ping message and the name
    /// of the corresponding host if it is not handled locally.
    /// </summary>
    public class Pong : SerializationCountMessage
    {
        public string? Message { get; set; }
    }
}