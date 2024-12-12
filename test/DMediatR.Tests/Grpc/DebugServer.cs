using Microsoft.Extensions.DependencyInjection;

namespace DMediatR.Tests.Grpc
{
    /// <summary>
    /// To be run from .\debug-server.ps1 while DMediatRNode is running in VS.
    /// </summary>
    [Category("DebugServer")]
    public class DebugServer
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
            var pongFromRemote = await Mediator.Send(new Ping("DebugServer"));
            Assert.That(pongFromRemote, Is.Not.Null);
        }
    }
}