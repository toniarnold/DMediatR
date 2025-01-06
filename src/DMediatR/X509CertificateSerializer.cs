using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal class X509CertificateSerializer : CustomSerializer<X509Certificate2>
    {
        private readonly PasswordOptions _options;

        public X509CertificateSerializer(IServiceProvider serviceProvider,
            IOptions<PasswordOptions> options) : base(serviceProvider)
        {
            _options = options.Value;
        }

        public override byte[] Serialize(Type type, object obj)
        {
            CheckType(type);
            var cert = (X509Certificate2)obj;
            var bytes = cert.Export(X509ContentType.Pkcs12, _options.Password);
            return base.Serialize(typeof(byte[]), bytes, checkType: false);
        }

        public override object Deserialize(Type type, byte[] bytes)
        {
            CheckType(type);
            var rawData = (byte[])base.Deserialize(typeof(byte[]), bytes, checkType: false);
            var cert = new X509Certificate2(rawData, _options.Password, X509KeyStorageFlags.Exportable);
            return cert;
        }
    }
}