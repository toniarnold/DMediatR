using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    /*
     * These class hierarchy models the certificate chain:
     *
     *                                                     -- Server Certificate
     *                                                    /
     *   Root Certificate <-- Intermediate Certificate <--
     *                                                    \
     *                                                     -- Client Certificate
     */

    internal abstract class CertificateRequest : ILock, IRequest<X509Certificate2>
    {
        /// <summary>
        /// When true, ignore RenewBeforeExpirationDays and validation for an
        /// explicit Renew request.
        /// </summary>
        public bool Renew { get; set; } = false;

        public HashSet<SemaphoreSlim>? HasLocked { get; set; } = [];
    }

    internal class RootCertificateRequest : CertificateRequest
    { }

    internal abstract class ChainedCertificateRequest : CertificateRequest
    {
        internal abstract CertificateRequest ParentCertificateRequest { get; }
    }

    internal class IntermediateCertificateRequest : ChainedCertificateRequest
    {
        internal override CertificateRequest ParentCertificateRequest
        { get { return new RootCertificateRequest(); } }
    }

    internal class ClientCertificateRequest : ChainedCertificateRequest
    {
        internal override CertificateRequest ParentCertificateRequest
        { get { return new IntermediateCertificateRequest(); } }
    }

    internal class ServerCertificateRequest : ChainedCertificateRequest
    {
        internal override IntermediateCertificateRequest ParentCertificateRequest
        { get { return new IntermediateCertificateRequest(); } }
    }
}