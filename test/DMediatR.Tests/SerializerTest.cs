using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR.Tests
{
    public class SerializerTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            _serviceProvider = sc.AddDMediatR(cfg)
                .AddLogging(builder => builder.AddNUnit())
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        public class Mother : IRequest
        {
            public List<Child> Children { get; set; } = [];
        }

        public class Child
        {
            public string? Name { get; set; }
            public Mother? Parent { get; set; }
        }

        public class Boy : Child
        { }

        public class Girl : Child
        { }

        [Test]
        public void SerializeDeserializeMixedList()
        {
            // setup with direct instantiation
            BinarySerializer serializer = new();

            Mother mother = new();
            Girl girl = new() { Name = "Lucy" };
            Boy boy = new() { Name = "John" };
            // This cyclic reference would cause the serializer to ignore them:
            //girl.Parent = mother;
            //boy.Parent = mother;
            mother.Children.Add(girl);
            mother.Children.Add(boy);

            // methods under test
            var bytes = serializer.Serialize(mother);
            var copy = serializer.Deserialize<Mother>(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(copy, Is.Not.Null);
                Assert.That(copy!.Children, Has.Exactly(2).Items);

                Assert.That(copy.Children[0].Name, Is.EqualTo("Lucy"));
                Assert.That(copy.Children[1].Name, Is.EqualTo("John"));

                // MessagePack deserializes them as Child, not as concrete Boy/Girl
                //Assert.That(copy.Children[0], Is.TypeOf<Girl>());
                //Assert.That(copy.Children[1], Is.TypeOf<Boy>());
            });
        }

        [Test]
        public void GetResponseTypeTest()
        {
            var request = new RootCertificateRequest();
            var responseType = request.GetResponseType();
            Assert.That(responseType, Is.Not.Null);
            Assert.That(responseType, Is.EqualTo(typeof(X509Certificate2)));
        }

        public class OneWay : IRequest
        { }

        [Test]
        public void GetResponseTypeVoidTest()
        {
            var request = new OneWay();
            var responseType = request.GetResponseType();
            Assert.That(responseType, Is.Null);
        }

        [Test]
        public void SerializeDeserializeX509Certificate()

        {
            // Check direct case and general case which needs to look up the direct serializer
            var serializers = new ISerializer[]
            {
                _serviceProvider.GetRequiredKeyedService<ISerializer>(X509CertificateSerializer.Type),
                _serviceProvider.GetRequiredService<ISerializer>()
            };

            foreach (var serializer in serializers)
            {
                var rootGen = _serviceProvider.GetRequiredService<RootCertificateProvider>();
                var rootReq = new RootCertificateRequest();
                var rootCert = rootGen.Generate(rootReq);

                // method under test
                var bytes = serializer.Serialize(rootCert);
                var copy = serializer.Deserialize<X509Certificate2>(bytes);

                // assertion
                Assert.That(rootCert.Thumbprint, Is.EqualTo(((X509Certificate2)copy).Thumbprint), $" with {serializer.GetType().Name}");
            }
        }

        [Test]
        public void CustomSerializeWrongTypeThrows()
        {
            // Directly instantiate the custom serializer to feed it with the wrong type:
            var x509Serializer = new X509CertificateSerializer(
                _serviceProvider,
                Options.Create(new CertificateOptions()));
            //x509Serializer.Serialize(new OneWay());
            Assert.That(() => x509Serializer.Serialize(new OneWay()),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void SerializedInterfaceWrongTypeThrows()
        {
            var serializedInterface = new ILockISerializedInterface();
            //serializedInterface.Dehydrate(new object());
            Assert.That(() => serializedInterface.Dehydrate(new object()),
                Throws.TypeOf<ArgumentException>());
        }
    }
}