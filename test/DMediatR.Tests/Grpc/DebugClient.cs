using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests.Grpc
{
    /// <summary>
    /// Expects a running DMediatRNode started via .\debug-client.ps1
    /// </summary>
    [Category("DebugClient")]
    public class DebugClient
    {
        private IMediator Mediator => SetUp.ServiceProvider!.GetRequiredService<IMediator>();

        [OneTimeSetUp]
        public void SetUpServices()
        {
            SetUp.SetUpDMediatRServices("RemotePing");
            SetUp.SetUpInitialCertificates();
        }

        [Test]
        public async Task Ping()
        {
            var pongFromRemote = await Mediator.Send(new Ping("DebugClient"));
            Assert.That(pongFromRemote, Is.Not.Null);
        }
    }
}