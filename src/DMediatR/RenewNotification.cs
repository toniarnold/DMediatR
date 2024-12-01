namespace DMediatR
{
    internal abstract class RenewNotification : ICorrelatedNotification
    {
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Duration the CorrelationGuid should stay in the cache. Required to prevent it from indefinitely growing.
        /// Afterwards, received duplicate copies of a notifications can no more be correlated and thus ignored.
        /// Defaults to 1 day, but can be adjusted to specific workloads here.
        /// </summary>
        public static TimeSpan MaxLatency { get; set; } = new(1, 0, 0, 0);
    }

    internal class RenewRootCertificateNotification : RenewNotification
    { }

    internal class RenewIntermediateCertificateNotification : RenewNotification
    { }

    internal class RenewServerCertificateNotification : RenewNotification
    { }

    internal class RenewClientCertificateNotification : RenewNotification
    { }
}