﻿using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Remote("ServerCertifier")]
    internal class ServerCertificateProviderRemote : ServerCertificateProvider, IRemote
    {
        public ServerCertificateProviderRemote(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<ServerCertificateProvider> logger)
                : base(remote, createCert, hostOptions, ioCert, logger) { }

        public override async Task<X509Certificate2> Handle(ServerCertificateRequest request, CancellationToken cancellationToken)
        {
            return await this.SendRemote(request, cancellationToken);
        }
    }
}