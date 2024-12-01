using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    [Local("ServerCertifier")]
    internal class ServerCertificateProvider : ChainedCertificateProvider,
        IRequestHandler<ServerCertificateRequest, X509Certificate2>,
        INotificationHandler<RenewServerCertificateNotification>
    {
        private readonly CreateCertificatesClientServerAuth _createCert;
        private static SemaphoreSlim _fileLock = new(1, 1);
        private string CommonName => $"{RemoteName}";
        private string FriendlyName => $"DMediatR {CommonName} server L3 certificate";

        public ServerCertificateProvider(
            CreateCertificatesClientServerAuth createCert,
            IOptions<HostOptions> hostOptions,
            IOptions<CertificateOptions> certOptions,
            IOptions<RemotesOptions> remotesOptions,
            IMediator mediator,
            ISerializer serializer,
            IGrpcChannelPool channel,
            ImportExportCertificate ioCert)
                : base(hostOptions, certOptions, remotesOptions, mediator, serializer, channel, ioCert)
        {
            _createCert = createCert;
        }

        public static string CertificateFilename(IConfiguration cfg)
        {
            string certName = LocalAttribute.RemoteName(typeof(ServerCertificateProvider))!;
            var options = GetOptionsFrom(cfg);
            string fileName = Path.GetFullPath(Path.Join(options.FilePath, $"{options.FilenamePrefix}-{certName}.pfx"));
            return fileName;
        }

        public static string OldCertificateFilename(IConfiguration cfg)
        {
            string certName = LocalAttribute.RemoteName(typeof(ServerCertificateProvider))!;
            var options = GetOptionsFrom(cfg);
            string fileName = Path.Join(options.FilePath, $"{options.FilenamePrefix}-{certName}-old.pfx");
            return fileName;
        }

        /// <summary>
        /// Static method called during service configuration for obtaining the locally stored current server certificate
        /// </summary>
        /// <param name="cfg">WebApplicationBuilder.Configuration</param>
        /// <returns></returns>
        public static X509Certificate2 LoadCertificate(IConfiguration cfg)
        {
            var options = GetOptionsFrom(cfg);
            string fileName = CertificateFilename(cfg);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(
                    $"The live server certificate {fileName} must initially be deployed to the Certificate:FilePath folder.", fileName);
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
            var fileName = OldCertificateFilename(cfg);
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(
                    "The certificate renewal server certificate {fileName} must initially be deployed to the Certificate:FilePath folder.", fileName);
            }
            return new X509Certificate2(File.ReadAllBytes(fileName), options.Password);
        }

        internal static CertificateOptions GetOptionsFrom(IConfiguration cfg)
        {
            return cfg.GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;
        }

        private static X509Certificate2 LoadCertificate(IConfiguration cfg, string FileName)
        {
            return new X509Certificate2(File.ReadAllBytes(FileName), GetOptionsFrom(cfg).Password);
        }

        public virtual async Task<X509Certificate2> Handle(ServerCertificateRequest request, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                return await RequestCertificate(request, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }

        async Task INotificationHandler<RenewServerCertificateNotification>.Handle(RenewServerCertificateNotification notification, CancellationToken cancellationToken)
        {
            if (File.Exists(FileName))
            {
                await base.Generate(new ServerCertificateRequest(), cancellationToken);
            }
        }

        internal override X509Certificate2 Generate(ChainedCertificateRequest request, X509Certificate2 parentCert)
        {
            var cert = _createCert.NewServerChainedCertificate(
               new DistinguishedName { CommonName = CommonName },
                new ValidityPeriod
                {
                    ValidFrom = DateTime.UtcNow,
                    ValidTo = DateTime.UtcNow.AddDays((int)Options.ValidDays!)
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
    }
}