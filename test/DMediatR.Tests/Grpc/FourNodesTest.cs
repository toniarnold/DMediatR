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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace DMediatR.Tests.Grpc
{
    [Category("Integration")]
    public class FourNodesTest
    {
        private CertificateOptions CertificateOptions =>
            SetUp.ServiceProvider!.GetRequiredService<IConfiguration>()
                .GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;

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

            await When_RootCertificateIsRenewed();
            await Then_DMediatRNodeRechable(); // still reachable, as the chain is not verified
            await When_IntermediateCertificateIsRenewed();
            await Then_DMediatRNodeRechable();
            await When_ServerCertificateIsRenewed();
            await Then_DMediatRNodeRechable();
        }

        #region Given

        private IMediator Mediator => SetUp.ServiceProvider!.GetRequiredService<IMediator>();

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
            DeployCertificate("DMediatR-Server.pfx", "RootCertifier");
            DeployCertificate("DMediatR-Server.pfx", "IntermediateCertifier");
            DeployCertificate("DMediatR-Server.pfx", "ServerCertifier");
            DeployCertificate("DMediatR-Server.pfx", "ClientCertifier");

            // The same client certificate used by all nodes.
            DeployCertificate("DMediatR-Client.pfx", "RootCertifier");
            DeployCertificate("DMediatR-Client.pfx", "IntermediateCertifier");
            DeployCertificate("DMediatR-Client.pfx", "ServerCertifier");
            DeployCertificate("DMediatR-Client.pfx", "ClientCertifier");

            // The intermediate certificate without key is required by all nodes
            // for validation.
            DeployCertificate("DMediatR-Intermediate.crt", "RootCertifier");
            DeployCertificate("DMediatR-Intermediate.crt", "IntermediateCertifier");
            DeployCertificate("DMediatR-Intermediate.crt", "ServerCertifier");
            DeployCertificate("DMediatR-Intermediate.crt", "ClientCertifier");

            // The self signed root certificate and the intermediate certificate
            // with key are not required for validation.
            DeployCertificate("DMediatR-Intermediate.pfx", "IntermediateCertifier");
            DeployCertificate("DMediatR-Root.pfx", "RootCertifier");
        }

        private void DeployCertificate(string certificate, string node)
        {
            var path = CertificateOptions.FilePath!;

            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, certificate), true);
            var oldCertificate = Regex.Replace(certificate, @".(\w\w\w)$", @"-old.$1");
            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, oldCertificate), true);
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
            using var httpClient = await SetUp.GetHttpClientAsync();
            var response = await httpClient.GetStringAsync("https://localhost:18007/"); // appsettings.RemotePing.json
            Assert.That(response, Is.EqualTo("DMediatR gRPC endpoint"));
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

        #endregion Then
    }
}