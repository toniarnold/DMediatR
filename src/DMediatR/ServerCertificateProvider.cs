using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("ServerCertifier")]
    internal class ServerCertificateProvider : ChainedCertificateProvider,
        IRequestHandler<ServerCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewServerCertificateNotification>
    {
        private const string ThisCertificateName = "Server"; // name required by static LoadCertificate()
        protected override string? CertificateName => ThisCertificateName; // name required by the base CertificateProvider

        private readonly CreateCertificatesClientServerAuth _createCert;
        private static SemaphoreSlim _fileLock = new(1, 1);
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} server L3 certificate";

        public ServerCertificateProvider(Remote remote,
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            ImportExportCertificate ioCert,
            ILogger<CertificateProvider> logger
            )
                : base(remote, hostOptions, ioCert, logger)
        {
            _createCert = createCert;
        }

        public virtual async Task<X509Certificate2> Handle(ServerCertificateRequest request, CancellationToken cancellationToken)
        {
            var locked = await request.Lock(_fileLock, cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { if (locked) _fileLock.Release(); }
        }

        public virtual async Task Handle(RenewServerCertificateNotification notification, CancellationToken cancellationToken)
        {
            var locked = await notification.Lock(_fileLock, cancellationToken);
            try
            {
                if (File.Exists(FileNamePfx))
                {
                    _logger.LogDebug("{notification}: renew existing certificate", notification.GetType().Name);
                    await base.Generate(new ServerCertificateRequest() { Renew = true, HasLocked = notification.HasLocked }, cancellationToken);
                }
            }
            finally { if (locked) _fileLock.Release(); }
        }

        /// <summary>
        /// Static method called during WebApplicationBuilder configuration for
        /// obtaining the locally stored current server certificate.
        /// </summary>
        /// <param name="cfg">WebApplicationBuilder.Configuration</param>
        /// <returns></returns>
        public static X509Certificate2 LoadCertificate(IConfiguration cfg)
        {
            var options = GetOptionsFrom(cfg);
            string fileName = CertificateFilename(options);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(
                    $"The server certificate {fileName} must initially be deployed to the Certificate:FilePath folder.", fileName);
            }
            return new X509Certificate2(File.ReadAllBytes(fileName), options.Password);
        }

        /// <summary>
        /// Static method called during service configuration for obtaining the locally stored former server certificate
        /// </summary>
        /// <param name="cfg">WebApplicationBuilder.Configuration</param>
        /// <returns></returns>
        public static X509Certificate2 LoadCertificateOld(IConfiguration cfg)
        {
            var options = GetOptionsFrom(cfg);
            var fileName = OldCertificateFilename(options);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(
                    "The certificate renewal server certificate {fileName} must initially be deployed to the Certificate:FilePath folder.", fileName);
            }
            return new X509Certificate2(File.ReadAllBytes(fileName), options.Password);
        }

        public override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewServerChainedCertificate(
               new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)(Options.ServerCertificateValidDays ?? Options.ValidDays!))
                },
                Options.HostName,
                parentCert);

#if OS_WINDOWS
#pragma warning disable CA1416
            cert.FriendlyName = FriendlyName;
#pragma warning restore CA1416
#endif
            return cert;
        }

        private static CertificateOptions GetOptionsFrom(IConfiguration cfg)
        {
            return cfg.GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;
        }

        private static string CertificateFilename(CertificateOptions options)
        {
            string fileName = Path.GetFullPath(Path.Join(options.FilePath, $"{options.FilenamePrefix}-{ThisCertificateName}.pfx"));
            return fileName;
        }

        private static string OldCertificateFilename(CertificateOptions options)
        {
            string fileName = Path.Join(options.FilePath, $"{options.FilenamePrefix}-{ThisCertificateName}-old.pfx");
            return fileName;
        }
    }
}