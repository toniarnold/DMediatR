using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal class X509CertificateSerializer : Serializer<X509Certificate2>
    {
        private readonly ISerializer _rawSerializer;
        private readonly CertificateOptions _options;

        public X509CertificateSerializer(
            IServiceProvider serviceProvider,
            IOptions<CertificateOptions> options) : base(serviceProvider)
        {
            _rawSerializer = serviceProvider.GetRequiredService<ISerializer>();
            _options = options.Value;
        }

        public override byte[] Serialize(object obj)
        {
            CheckType(obj.GetType());
            return _rawSerializer.Serialize(((X509Certificate2)obj).RawData);
        }

        public override T Deserialize<T>(byte[] bytes)
        {
            CheckType(typeof(T));
            return (T)Deserialize(typeof(T), bytes);
        }

        public override object Deserialize(Type type, byte[] bytes)
        {
            CheckType(type);
            var rawData = _rawSerializer.Deserialize<byte[]>(typeof(byte[]), bytes);
            var cert = new X509Certificate2(rawData, _options.Password);
            return (dynamic)cert;
        }
    }
}