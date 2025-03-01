﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMediatR.Tests.Grpc
{
    [Category("Integration")]
    public class ServerTest
    {
        public const string FILENAME_SVG = "remotes.Monolith.svg";

        [OneTimeSetUp]
        public void SetUpInitialCertificatesStartServer()
        {
            SetUp.SetUpDMediatRServices("Monolith"); // generate locally
            SetUp.SetUpInitialCertificates();
            SetUp.SetUpDMediatRServices("RemoteAllCerts"); // smoke test mode, as the remote shares the cert location

            // <startserver>
            SetUp.StartServer("Monolith", 18001, 18002); // or ./start Monolith
            SetUp.AssertServersStarted();
            // </startserver>
        }

        [OneTimeTearDown]
        public void StopServers()
        {
            SetUp.StopAllServers();
        }

        [Test]
        public async Task GrpcChannelPoolTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ServerCertificateRequest());
            await mediator.Send(new ServerCertificateRequest());
        }

        [Test]
        public async Task PingPongTest()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var pongFromRemote = await mediator.Send(new Ping("from NUnit"));
            Assert.That(pongFromRemote, Is.Not.Null);
            Assert.That(pongFromRemote.Count, Is.EqualTo(2));
            Assert.That(pongFromRemote.Message, Is.EqualTo("Pong 2 hops from NUnit via Monolith via localhost:8081"));
        }

        // Usually gets the certificate from the local ./cert cache:
        [Test]
        public async Task GetRemoteClientCert()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            var certFromRemote = await mediator.Send(new ClientCertificateRequest());
            Assert.That(certFromRemote, Is.Not.Null);
            Assert.That(certFromRemote.Subject, Is.EqualTo("CN=ClientCertifier"));
        }

        [Test]
        public async Task ForceRenewServerCertRemote()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();
            // Requesting it twice yields the same certificate from the filesysem.
            var oldCertFromRemote = await mediator.Send(new ServerCertificateRequest());
            var oldCertDouble = await mediator.Send(new ServerCertificateRequest());
            Assert.That(oldCertFromRemote.Thumbprint, Is.EqualTo(oldCertDouble.Thumbprint));

            await mediator.Publish(new RenewServerCertificateNotification());
            // After explicit renewal the gRPC server is restarted and yields a new certificate.
            TestSetUp.WaitForServerPort(18001);
            var newCertFromRemote = await mediator.Send(new ServerCertificateRequest());
            Assert.That(newCertFromRemote.Thumbprint, Is.Not.EqualTo(oldCertDouble.Thumbprint));
        }

        [Ignore("not functional yet")]
        [Test]
        public async Task ForceRenewCertChainRemote()
        {
            var mediator = SetUp.ServiceProvider.GetRequiredService<IMediator>();

            var oldRoot = await mediator.Send(new RootCertificateRequest());
            var oldInter = await mediator.Send(new IntermediateCertificateRequest());
            var oldServer = await mediator.Send(new ServerCertificateRequest());
            var oldClient = await mediator.Send(new ClientCertificateRequest());

            var sameRoot = await mediator.Send(new RootCertificateRequest());
            var sameInter = await mediator.Send(new IntermediateCertificateRequest());
            var sameServer = await mediator.Send(new ServerCertificateRequest());
            var sameClient = await mediator.Send(new ClientCertificateRequest());

            Assert.Multiple(() =>
            {
                Assert.That(sameRoot.Thumbprint, Is.EqualTo(oldRoot.Thumbprint));
                Assert.That(sameInter.Thumbprint, Is.EqualTo(oldInter.Thumbprint));
                Assert.That(sameServer.Thumbprint, Is.EqualTo(oldServer.Thumbprint));
                Assert.That(sameClient.Thumbprint, Is.EqualTo(oldClient.Thumbprint));
            });

            await mediator.Publish(new RenewRootCertificateNotification());

            // But connecting the next time will fetch a new client certificate.
            await mediator.Send(new Ping("after RenewRootCertificateNotification"));

            var newRoot = await mediator.Send(new RootCertificateRequest());
            var newInter = await mediator.Send(new IntermediateCertificateRequest());
            var newServer = await mediator.Send(new ServerCertificateRequest());
            var newClient = await mediator.Send(new ClientCertificateRequest());

            Assert.Multiple(() =>
            {
                Assert.That(newRoot.Thumbprint, Is.Not.EqualTo(oldRoot.Thumbprint), "Root");
                Assert.That(newInter.Thumbprint, Is.Not.EqualTo(oldInter.Thumbprint), "Intermediate");
                Assert.That(newServer.Thumbprint, Is.Not.EqualTo(oldServer.Thumbprint), "Server");
                Assert.That(newClient.Thumbprint, Is.Not.EqualTo(oldClient.Thumbprint), "Client");
            });
        }

        [Test]
        public async Task GetRemotesSvg()
        {
            using var httpClient = await TestSetUp.GetHttpClientAsync();
            var host = SetUp.ServiceProvider.GetRequiredService<IOptions<HostOptions>>().Value;
            var svg = await httpClient.GetStringAsync("https://localhost:18001/remotes.svg");
            Assert.That(svg, Does.Contain("<svg"));
            SetUp.SaveOutput(FILENAME_SVG, svg);
        }
    }
}