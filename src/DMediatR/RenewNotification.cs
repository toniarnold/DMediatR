namespace DMediatR
{
    /// <summary>
    /// Base class for MediatR notifications enforcing certificate renewal.
    /// Requires explicitly setting "RenewFirewallEnabled": "False" in the
    /// "Certificate" configuration section, otherwise it is ignored.
    /// </summary>
    public abstract class RenewNotification : SerializationCountMessage, ILock, ICorrelatedNotification
    {
        public Guid CorrelationGuid { get; init; } = Guid.NewGuid();
        public HashSet<SemaphoreSlim>? HasLocked { get; set; } = [];
    }

    /// <summary>
    /// Renew the root certificate on the node which has it stored.
    /// </summary>
    public class RenewRootCertificateNotification : RenewNotification
    { }

    /// <summary>
    /// Renew the intermediate certificate on the node which has it stored.
    /// </summary>
    public class RenewIntermediateCertificateNotification : RenewNotification
    { }

    /// <summary>
    /// Renew the server certificates on all nodes acting as server.
    /// </summary>
    public class RenewServerCertificateNotification : RenewNotification
    { }

    /// <summary>
    /// Renew the client certificates on all reachable nodes acting as client.
    /// </summary>
    public class RenewClientCertificateNotification : RenewNotification
    { }
}