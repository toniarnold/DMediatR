namespace DMediatRNode1
{
    public class Notification1 : ICorrelatedNotification
    {
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
        public string? Message { get; set; }
    }
}