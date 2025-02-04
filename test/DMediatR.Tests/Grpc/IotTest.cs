using Iot;
using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests.Grpc
{
    [Category("IoT")]
    [Remote("CpuTemp")]
    public class IotTest : IRemote
    {
        public Remote Remote { get; set; } = default!;

        [OneTimeSetUp]
        public void SetUpInitialCertificatesRemote()
        {
            SetUp.SetUpDMediatRServices("Iot");
            SetUp.SetUpInitialCertificates();
            Remote = SetUp.ServiceProvider.GetRequiredService<Remote>();
        }

        // <cputemp>
        [Test]
        public async Task CpuTempReachable()
        {
            using var httpClient = await TestSetUp.GetHttpClientAsync();
            var response = await httpClient.GetStringAsync(Remote.Remotes["CpuTemp"].Address);
            Assert.That(response, Is.EqualTo("DMediatR gRPC endpoint"));
        }

        [Test]
        public async Task GetRemoteTemp()
        {
            var temperature = await this.SendRemote(new TempRequest(), CancellationToken.None);
            TestContext.WriteLine($"CpuTemp of {Remote.Remotes["CpuTemp"].Address} is {temperature,0:f1} ℃.");
        }

        // </cputemp>
    }
}