using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    internal class ServerCertificateFileWatcher : BackgroundService
    {
        private readonly ILogger<ServerCertificateFileWatcher> _logger;
        private readonly HostOptions _hostOptions;
        private readonly ServerCertificateProvider _certificateProvider;
        private readonly IHostApplicationLifetime _appLifetime;

        public ServerCertificateFileWatcher(
            ILogger<ServerCertificateFileWatcher> logger,
            IOptions<HostOptions> hostOptions,
            ServerCertificateProvider certificateProvider,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _hostOptions = hostOptions.Value;
            _certificateProvider = certificateProvider;
            _appLifetime = appLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            string activeCertFileName = "";
            switch (_hostOptions.GrpcPort)
            {
                case GrpcPort.UseDefault:
                    activeCertFileName = _certificateProvider.FileName;
                    break;

                case GrpcPort.UseRenew:
                    activeCertFileName = _certificateProvider.FileNameOld;
                    break;
            }
            using var certWatcher = new FileSystemWatcher(
                Path.GetDirectoryName(activeCertFileName)!,
                Path.GetFileName(activeCertFileName)!
                );
            certWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            certWatcher.Changed += OnCertChanged;
            certWatcher.Created += OnCertChanged;
            certWatcher.Deleted += OnCertChanged;
            certWatcher.Renamed += OnCertChanged;
            certWatcher.EnableRaisingEvents = true;

            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (OperationCanceledException) { } // restarting is normal control flow
        }

        private void OnCertChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Restarting as the Certificate file changed: {path}", e.FullPath);
            _appLifetime.StopApplication();
        }
    }
}