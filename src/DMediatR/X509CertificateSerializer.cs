using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal class X509CertificateSerializer : CustomSerializer<X509Certificate2>
    {
        private readonly PasswordOptions _options;

        public X509CertificateSerializer(
            IServiceProvider serviceProvider,
            IOptions<PasswordOptions> options) : base(serviceProvider)
        {
            _options = options.Value;
        }

        public override byte[] Serialize(Type type, object obj)
        {
            CheckType(type);
            return base.Serialize(typeof(byte[]), ((X509Certificate2)obj).RawData, checkType: false);
        }

        public override object Deserialize(Type type, byte[] bytes)
        {
            CheckType(type);
            var rawData = (byte[])base.Deserialize(typeof(byte[]), bytes, checkType: false);
            var cert = new X509Certificate2(rawData, _options.Password);
            return (dynamic)cert;
        }
    }
}