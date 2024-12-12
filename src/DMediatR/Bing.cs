namespace DMediatR
{
    /// <summary>
    /// Broadcast Ping as MediatR INotification.
    /// </summary>
    public class Bing : SerializationCountMessage, ICorrelatedNotification
    {
        public string? Message { get; set; }
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();

        public Bing()
        {
        }

        public Bing(string message)
        {
            Message = message;
        }
    }
}