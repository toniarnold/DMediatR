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

    internal abstract class CertificateRequest : IRequest<X509Certificate2>
    { }

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