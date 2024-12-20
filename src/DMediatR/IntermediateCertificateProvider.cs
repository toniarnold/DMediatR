﻿using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("IntermediateCertifier")]
    internal class IntermediateCertificateProvider : ChainedCertificateProvider,
        IRequestHandler<IntermediateCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewIntermediateCertificateNotification>
    {
        private readonly CreateCertificatesClientServerAuth _createCert;
        private static SemaphoreSlim _fileLock = new(1, 1);

        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} intermediate L2 certificate";

        public IntermediateCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger)
                : base(remote, hostOptions, ioCert, logger)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(IntermediateCertificateRequest request, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }

        public async Task Handle(RenewIntermediateCertificateNotification notification, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(FileName))
                {
                    _logger.LogDebug("Renewing Intermediate Certificate");
                    await base.Generate(new IntermediateCertificateRequest(), cancellationToken);
                }
            }
            finally { _fileLock.Release(); }
        }

        internal override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewIntermediateChainedCertificate(
                new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)Options.ValidDays!)
                },
                2,
                Options.HostName,
                parentCert);
            return cert;
        }
    }
}