namespace DMediatR
{
    /// <summary>
    /// Broadcast Ping as MediatR INotification.
    /// </summary>
    public class Bing : SerializationCountMessage, INotification
    {
        public string? Message { get; set; }
    }
}