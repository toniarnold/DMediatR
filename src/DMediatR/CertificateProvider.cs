using CertificateManager;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal abstract class CertificateProvider
    {
        protected readonly Remote _remote;
        protected readonly HostOptions _hostOptions;
        protected readonly ImportExportCertificate _importExportCertificate;
        protected readonly ILogger<CertificateProvider> _logger;

        protected CertificateProvider(Remote remote,
                IOptions<HostOptions> hostOptions,
                ImportExportCertificate ioCert,
                ILogger<CertificateProvider> logger)
        {
            _remote = remote;
            _hostOptions = hostOptions.Value;
            _importExportCertificate = ioCert;
            _logger = logger;
        }

        public Remote Remote => _remote;
        public CertificateOptions Options => _remote.CertificateOptions;
        protected string? RemoteName => LocalAttribute.RemoteName(this.GetType());

        internal string FileName =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{RemoteName}.pfx");

        internal string FileNameOld =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{RemoteName}-old.pfx");

        public async Task<(bool, X509Certificate2?)> TryLoad(CancellationToken cancellationToken)
        {
            if (File.Exists(FileName))
            {
                var bytes = await File.ReadAllBytesAsync(FileName, cancellationToken);
                var cert = new X509Certificate2(bytes, Options.Password, X509KeyStorageFlags.Exportable);
                return (true, cert);
            }
            else
            {
                return (false, null);
            }
        }

        internal async Task Save(byte[] bytes, CancellationToken cancellationToken)
        {
            if (File.Exists(FileName))
            {
                File.Move(FileName, FileNameOld, true);
            }
            await File.WriteAllBytesAsync(FileName, bytes, cancellationToken);
        }
    }
}