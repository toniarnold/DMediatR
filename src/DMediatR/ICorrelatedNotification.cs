namespace DMediatR
{
    /// <summary>
    /// Correlated notification with a CorrelationGuid from ICorrelatedMessage.
    /// </summary>
    public interface ICorrelatedNotification : ICorrelatedMessage, INotification
    {
    }
}