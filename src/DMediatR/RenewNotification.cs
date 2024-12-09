namespace DMediatR
{
    internal abstract class RenewNotification : ICorrelatedNotification
    {
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
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