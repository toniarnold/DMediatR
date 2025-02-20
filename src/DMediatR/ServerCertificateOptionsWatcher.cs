using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    /// <summary>
    /// BackgroundService monitoring the server certificate and all DMediatR
    /// options. If any of the certificate or configuration files change,
    /// restarts the server using IHostApplicationLifetime.StopApplication()
    /// </summary>
    internal class ServerCertificateOptionsWatcher : BackgroundService
    {
        private readonly ILogger<ServerCertificateOptionsWatcher> _logger;
        private readonly HostOptions _hostOptions;
        private readonly GrpcOptions _grpcOptions;
        private readonly ServerCertificateProvider _certificateProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly OptionsMonitor _optionsMonitor;

        public ServerCertificateOptionsWatcher(
            ILogger<ServerCertificateOptionsWatcher> logger,
            IOptions<HostOptions> hostOptions,
            IOptions<GrpcOptions> grpcOptions,
            ServerCertificateProvider certificateProvider,
            IHostApplicationLifetime appLifetime,
            OptionsMonitor optionsMonitor)
        {
            _logger = logger;
            _hostOptions = hostOptions.Value;
            _grpcOptions = grpcOptions.Value;
            _certificateProvider = certificateProvider;
            _appLifetime = appLifetime;
            _optionsMonitor = optionsMonitor;
        }

        public override void Dispose()
        {
            _optionsMonitor.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_grpcOptions.ServerCertificateWatcherEnabled)
            {
                this.Enable();
            }
            if (_grpcOptions.OptionsMonitorEnabled)
            {
                _optionsMonitor.Enable();
            }
            if (_grpcOptions.ServerCertificateWatcherEnabled ||
                _grpcOptions.OptionsMonitorEnabled)
            {
                try
                {
                    await Task.Delay(-1, cancellationToken);
                }
                catch (OperationCanceledException) { } // restarting is normal control flow
            }
        }

        private void Enable()
        {
            string activeCertFileName = "";
            switch (_hostOptions.GrpcPort)
            {
                default:
                case GrpcPort.UseDefault:
                    activeCertFileName = _certificateProvider.FileNamePfx;
                    break;

                case GrpcPort.UseRenew:
                    activeCertFileName = _certificateProvider.FileNameOldPfx;
                    break;
            }
            using var certWatcher = new FileSystemWatcher(
                Path.GetDirectoryName(activeCertFileName)!,
                Path.GetFileName(activeCertFileName)!
                );
            certWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            certWatcher.Changed += OnCertChangedRestart;
            certWatcher.Created += OnCertChangedRestart;
            certWatcher.Deleted += OnCertChangedRestart;
            certWatcher.Renamed += OnCertChangedRestart;
            certWatcher.EnableRaisingEvents = true;
        }

        private void OnCertChangedRestart(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Restarting {host} as the certificate file changed: {path}", _hostOptions.Host, e.FullPath);
            _appLifetime.StopApplication();
        }
    }
}