using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests
{
    public class ILockSerializedInterfaceTest
    {
        private ServiceProvider _serviceProvider;

        [OneTimeSetUp]
        public void SetUpDMediatRServices()

        {
            var cfg = Configuration.Get();
            ServiceCollection sc = new();
            _serviceProvider = sc.AddDMediatR(cfg)
                .BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void SerializeNotification()
        {
            var serializer = _serviceProvider.GetRequiredService<ISerializer>();
            var notification = new RenewClientCertificateNotification();
            notification.HasLocked!.Add(new SemaphoreSlim(1, 1));
            var bytes = serializer.Serialize(notification);
            var copy = serializer.Deserialize<RenewClientCertificateNotification>(bytes);
            Assert.That(copy.HasLocked!.Count, Is.EqualTo(0)); // rehydrated without locks
        }

        [Test]
        public void SerializeRequest()
        {
            var serializer = _serviceProvider.GetRequiredService<ISerializer>();
            var request = new RenewClientCertificateNotification();
            request.HasLocked!.Add(new SemaphoreSlim(1, 1));
            var bytes = serializer.Serialize(request);
            var copy = serializer.Deserialize<RenewClientCertificateNotification>(bytes);
            Assert.That(copy.HasLocked!.Count, Is.EqualTo(0)); // rehydrated without locks
        }
    }
}