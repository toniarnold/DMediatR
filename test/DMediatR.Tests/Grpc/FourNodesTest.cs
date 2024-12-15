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

namespace DMediatR.Tests.Grpc
{
    [Category("Integration")]
    public class FourNodesTest
    {
        private CertificateOptions CertificateOptions =>
            SetUp.ServiceProvider!.GetRequiredService<IConfiguration>()
                .GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;

        [OneTimeSetUp]
        public async Task Given_FourNodesRunning()
        {
            Given_InitialCertificatesChain();
            Given_CertificatesDistributedOffline();
            Given_FourServersStarted();
            Given_DMediatRClient();
        }

        [Test]
        public async Task TestFourNodesWithClient()
        {
            // Given_*
            await Then_DMediatRNodeRechable();
            await Then_DMediatRNodeAnswersPing();
            await Then_DmMediatRNodeForwardsBing(); // functional as long ClientCertifier is not configured in Remotes
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

            Assert.That(SetUp.ServerProcesses, Has.Count.EqualTo(4));
            for (int i = 0; i < 4; i++)
            {
                var process = SetUp.ServerProcesses[i];
                Assert.That(process.HasExited, Is.False, $"Process {i} was not started");
            }
        }

        /// <summary>
        /// Only the needed certificates are copied to the respective node.
        /// </summary>
        private void Given_CertificatesDistributedOffline()
        {
            // The same server certificates for all nodes
            DeployCertificate("DMediatR-ServerCertifier.pfx", "RootCertifier");
            DeployCertificate("DMediatR-ServerCertifier.pfx", "IntermediateCertifier");
            DeployCertificate("DMediatR-ServerCertifier.pfx", "ServerCertifier");
            DeployCertificate("DMediatR-ServerCertifier.pfx", "ClientCertifier");

            // The same client certificate used by all nodes
            DeployCertificate("DMediatR-ClientCertifier.pfx", "RootCertifier");
            DeployCertificate("DMediatR-ClientCertifier.pfx", "IntermediateCertifier");
            DeployCertificate("DMediatR-ClientCertifier.pfx", "ServerCertifier");
            DeployCertificate("DMediatR-ClientCertifier.pfx", "ClientCertifier");

            // The specifically created certificates not deployed yet that each node creates.
            DeployCertificate("DMediatR-RootCertifier.pfx", "RootCertifier");
            DeployCertificate("DMediatR-IntermediateCertifier.pfx", "IntermediateCertifier");
        }

        private void DeployCertificate(string certificate, string node)
        {
            var path = CertificateOptions.FilePath!;

            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, certificate), true);
            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, certificate.Replace(".pfx", "-old.pfx")), true);
        }

        private void Given_DMediatRClient()
        {
            SetUp.SetUpDMediatRServices("RemotePing");
        }

        #endregion Given

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
            Assert.That(pongFromRemote.Count, Is.EqualTo(2));   // 2 hops
            Assert.That(pongFromRemote.Message, Is.EqualTo("Pong 1 hops from NUnit via ClientCertifier"));
        }

        private async Task Then_DmMediatRNodeForwardsBing()
        {
            await Mediator.Publish(new Bing("from NUnit"));
        }

        #endregion Then

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }
    }
}