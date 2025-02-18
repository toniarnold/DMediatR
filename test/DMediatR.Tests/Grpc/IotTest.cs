using Iot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMediatR.Tests.Grpc
{
    [Category("IoT")]
    [Remote("CpuTemp")]
    public class IotTest : IRemote
    {
        public const string FILENAME_SVG = "remotes.Iot.svg";

        public Remote Remote { get; set; } = default!;

        [OneTimeSetUp]
        public void SetUpServices()
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
            Assert.That(response, Is.EqualTo("DMediatR on rpi:18001"));
        }

        [Test]
        public async Task GetRemoteTemp()
        {
            var temperature = await this.SendRemote(new TempRequest(), CancellationToken.None);
            TestContext.WriteLine($"CpuTemp of {Remote.Remotes["CpuTemp"].Address} is {temperature,0:f1} ℃.");
        }

        // </cputemp>

        [Test]
        public async Task GetRemotesSvg()
        {
            using var httpClient = await TestSetUp.GetHttpClientAsync();
            var host = SetUp.ServiceProvider.GetRequiredService<IOptions<HostOptions>>().Value;
            var svg = await httpClient.GetStringAsync("https://rpi.:18001/remotes.svg");
            Assert.That(svg, Does.Contain("<svg"));
            SetUp.SaveOutput(FILENAME_SVG, svg);
        }
    }
}