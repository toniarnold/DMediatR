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
    [Ignore("Access violation, stack overflow")]
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
            await Given_DMediatRClientRechable();
        }

        [Test]
        public async Task TestFourNodesWithClient()
        {
            await Task.CompletedTask;
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

            // The same client certificate used by all nodes except root.
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

        /// <summary>
        /// RemoteClientCert is the node this test itself is connected to.
        /// </summary>
        private void Given_DMediatRClient()
        {
            SetUp.SetUpDMediatRServices("RemoteClientCert");
        }

        /// <summary>
        /// Request a plausible client certificate from the ClientCertifier node.
        /// </summary>
        private async Task Given_DMediatRClientRechable()
        {
            var clientCertFromRemote = await Mediator.Send(new ClientCertificateRequest());
            Assert.That(clientCertFromRemote, Is.Not.Null);
            Assert.That(clientCertFromRemote.Subject, Is.EqualTo("CN=ClientCertifier"));
        }

        #endregion Given

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }
    }
}