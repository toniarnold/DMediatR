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
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public Remote Remote => _remote;
        public CertificateOptions Options => _remote.CertificateOptions;

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

        protected string? RemoteName => LocalAttribute.RemoteName(this.GetType());

        protected abstract string? CertificateName { get; }

        public string FileNamePfx =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{CertificateName}.pfx");

        public string FileNameOldPfx =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{CertificateName}-old.pfx");

        public string FileNameCrt =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{CertificateName}.crt");

        public string FileNameOldCrt =>
            Path.Join(Options.FilePath, $"{Options.FilenamePrefix}-{CertificateName}-old.crt");

        public async Task<(bool, X509Certificate2?)> TryLoad(CancellationToken cancellationToken)
        {
            return await TryLoad(GrpcPort.UseDefault, cancellationToken);
        }

        public async Task<(bool, X509Certificate2?)> TryLoad(GrpcPort usePort, CancellationToken cancellationToken)
        {
            switch (usePort)
            {
                default:
                case GrpcPort.UseDefault:
                    return await TryLoad(FileNamePfx, cancellationToken);

                case GrpcPort.UseRenew:
                    return await TryLoad(FileNameOldPfx, cancellationToken);
            }
        }

        public async Task<(bool, X509Certificate2?)> TryLoadCrt(GrpcPort usePort, CancellationToken cancellationToken)
        {
            switch (usePort)
            {
                default:
                case GrpcPort.UseDefault:
                    return await TryLoad(FileNameCrt, cancellationToken);

                case GrpcPort.UseRenew:
                    return await TryLoad(FileNameOldCrt, cancellationToken);
            }
        }

        public async Task<(bool, X509Certificate2?)> TryLoad(string fileName, CancellationToken cancellationToken)
        {
            if (File.Exists(fileName))
            {
                var bytes = await File.ReadAllBytesAsync(fileName, cancellationToken);
                var cert = new X509Certificate2(bytes, Options.Password, X509KeyStorageFlags.Exportable);
                return (true, cert);
            }
            else
            {
                return (false, null);
            }
        }

        public async Task SavePfx(byte[] bytesPfx, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(FileNamePfx))
                {
                    File.Move(FileNamePfx, FileNameOldPfx, true);
                }
                await File.WriteAllBytesAsync(FileNamePfx, bytesPfx, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }

        public async Task SaveCrt(byte[] bytesCrt, CancellationToken cancellationToken)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                if (File.Exists(FileNameCrt))
                {
                    File.Move(FileNameCrt, FileNameOldCrt, true);
                }
                await File.WriteAllBytesAsync(FileNameCrt, bytesCrt, cancellationToken);
            }
            finally { _fileLock.Release(); }
        }
    }
}