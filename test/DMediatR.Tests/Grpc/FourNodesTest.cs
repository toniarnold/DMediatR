/*
Spin up these four DMediatRNodes with these Ports and OldPorts all on localhost:

RootCertifier   IntermediateCertifier   ServerCertifier     ClientCertifier
18001           18003                   18005               18007
18002           18004                   18006               18008

The dependencies are the same as in the CertificateRequest classes, each node requests
their respective dependency from the corresponding remote node:

                                       - ServerCertifier
                                      /
RootCertifier <-- IntermediateCertifier <-
                                      \
                                       - ClientCertifier

As all nodes must answer gRPC requests, they are all configured to receive a server certificate
from the ServerCertifier and the client certificate from the ClientCertifier.
As they all use the same cert directory, the deploying the initial certificate chain is simple.

The test class itself acts as a gRPC client certified by the ClientCertifier.

The test structure follows loosely the Gherkin syntax: https://cucumber.io/docs/gherkin/reference/
*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMediatR.Tests.Grpc
{
    [Category("Integration")]
    public class FourNodesTest
    {
        public const string FILENAME_SVG = "remotes.FourNodes.svg";

        [OneTimeSetUp]
        public void Given_FourNodesRunning()
        {
            Given_InitialCertificatesChain();
            Given_CertificatesDistributedOffline();
            Given_FourServersStarted(); // or ./start RootCertifier IntermediateCertifier ServerCertifier ClientCertifier
            Given_DMediatRClient();
        }

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }

        [Test]
        public async Task TestFourNodesWithClient()
        {
            // Given_*
            await Then_DMediatRNodeRechable();
            await Then_DMediatRNodeAnswersPing();
            await Then_DmMediatRNodeForwardsBing(); // functional as long ClientCertifier is not configured in Remotes
            await Then_DMediatRNodeGraphAvailable();

            // not yet functional:
            /*
            await When_RootCertificateIsRenewed();
            await Then_DMediatRNodeRechable(); // still reachable, as the chain is not verified
            await When_IntermediateCertificateIsRenewed();
            await Then_DMediatRNodeRechable();
            await When_ServerCertificateIsRenewed();
            await Then_DMediatRNodeRechable();
            */
        }

        #region Given

        private IMediator Mediator => SetUp.ServiceProvider.GetRequiredService<IMediator>();

        /// <summary>
        /// The initial certificate chain is created on a monolith without any gRPC.
        /// </summary>
        private void Given_InitialCertificatesChain()
        {
            SetUp.SetUpDMediatRServices("Monolith");
            SetUp.SetUpInitialCertificates();
        }

        /// <summary>
        /// Start the four gRPC server nodes awaiting their respective port numbers to become reachable.
        /// </summary>
        private static void Given_FourServersStarted()
        {
            SetUp.StartServer("RootCertifier", 18001, 18002);
            SetUp.StartServer("IntermediateCertifier", 18003, 18004);
            SetUp.StartServer("ServerCertifier", 18005, 18006);
            SetUp.StartServer("ClientCertifier", 18007, 18008);
            SetUp.AssertServersStarted();
        }

        /// <summary>
        /// Only the needed certificates are copied to the respective node.
        /// </summary>
        private void Given_CertificatesDistributedOffline()
        {
            // The same server certificates for all nodes.
            SetUp.DeployCertificate("DMediatR-Server.pfx", "RootCertifier");
            SetUp.DeployCertificate("DMediatR-Server.pfx", "IntermediateCertifier");
            SetUp.DeployCertificate("DMediatR-Server.pfx", "ServerCertifier");
            SetUp.DeployCertificate("DMediatR-Server.pfx", "ClientCertifier");

            // The same client certificate used by all nodes.
            SetUp.DeployCertificate("DMediatR-Client.pfx", "RootCertifier");
            SetUp.DeployCertificate("DMediatR-Client.pfx", "IntermediateCertifier");
            SetUp.DeployCertificate("DMediatR-Client.pfx", "ServerCertifier");
            SetUp.DeployCertificate("DMediatR-Client.pfx", "ClientCertifier");

            // The intermediate certificate without key is required by all nodes
            // for validation.
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "RootCertifier");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "IntermediateCertifier");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "ServerCertifier");
            SetUp.DeployCertificate("DMediatR-Intermediate.crt", "ClientCertifier");

            // The self signed root certificate and the intermediate certificate
            // with key are not required for validation.
            SetUp.DeployCertificate("DMediatR-Intermediate.pfx", "IntermediateCertifier");
            SetUp.DeployCertificate("DMediatR-Root.pfx", "RootCertifier");
        }

        private void Given_DMediatRClient()
        {
            SetUp.SetUpDMediatRServices("RemotePing");
        }

        #endregion Given

        #region When

        public async Task When_RootCertificateIsRenewed()
        {
            await Mediator.Publish(new RenewRootCertificateNotification());
        }

        public async Task When_IntermediateCertificateIsRenewed()
        {
            await Mediator.Publish(new RenewIntermediateCertificateNotification());
        }

        public async Task When_ServerCertificateIsRenewed()
        {
            await Mediator.Publish(new RenewServerCertificateNotification());
        }

        #endregion When

        #region Then

        /// <summary>
        /// Side-effect free simple HTTP/2 request
        /// </summary>
        /// <returns></returns>
        private async Task Then_DMediatRNodeRechable()
        {
            using var httpClient = await TestSetUp.GetHttpClientAsync();
            var response = await httpClient.GetStringAsync("https://localhost:18007/"); // appsettings.RemotePing.json
            Assert.That(response, Is.EqualTo("DMediatR ClientCertifier on localhost:18007"));
        }

        private async Task Then_DMediatRNodeAnswersPing()
        {
            var pongFromRemote = await Mediator.Send(new Ping("from NUnit"));
            Assert.That(pongFromRemote, Is.Not.Null);
            Assert.That(pongFromRemote.Count, Is.EqualTo(2));
            Assert.That(pongFromRemote.Message, Is.EqualTo("Pong 2 hops from NUnit via ClientCertifier via localhost:8081"));
        }

        private async Task Then_DmMediatRNodeForwardsBing()
        {
            await Mediator.Publish(new Bing("from NUnit"));
        }

        private async Task Then_DMediatRNodeGraphAvailable()
        {
            using var httpClient = await TestSetUp.GetHttpClientAsync();
            var host = SetUp.ServiceProvider.GetRequiredService<IOptions<HostOptions>>().Value;
            var svg = await httpClient.GetStringAsync("https://localhost:18007/remotes.svg");
            Assert.That(svg, Does.Contain("<svg"));
            SetUp.SaveOutput(FILENAME_SVG, svg);
        }

        #endregion Then
    }
}