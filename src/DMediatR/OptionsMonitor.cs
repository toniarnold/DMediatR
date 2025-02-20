using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMediatR
{
    /// <summary>
    /// IOptionsMonitor for all appsettings.json options. Don't attempt with hot
    /// reload using IOptionsSnapshot<T>, as many options require an application
    /// restart anyway.
    /// </summary>
    internal class OptionsMonitor : IDisposable
    {
        private readonly ILogger<OptionsMonitor> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IOptionsMonitor<CertificateOptions> _certificateMonitor;
        private readonly IOptionsMonitor<GrpcOptions> _grpcMonitor;
        private readonly IOptionsMonitor<HostOptions> _hostMonitor;
        private readonly IOptionsMonitor<RemotesOptions> _remotesMonitor;
        private readonly object _restartLock = new();
        private bool _restarting = false;

        private IDisposable? OnCertificateChangeHandle { get; set; }
        private IDisposable? OnGrpcChangeHandle { get; set; }
        private IDisposable? OnHostChangeHandle { get; set; }
        private IDisposable? OnRemotesChangeHandle { get; set; }

        public OptionsMonitor(
            ILogger<OptionsMonitor> logger,
            IHostApplicationLifetime appLifetime,
            IOptionsMonitor<CertificateOptions> certificateMonitor,
            IOptionsMonitor<GrpcOptions> grpcMonitor,
            IOptionsMonitor<HostOptions> hostMonitor,
            IOptionsMonitor<RemotesOptions> remotesMonitor
            )
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _certificateMonitor = certificateMonitor;
            _grpcMonitor = grpcMonitor;
            _hostMonitor = hostMonitor;
            _remotesMonitor = remotesMonitor;
        }

        public void Dispose()
        {
            OnCertificateChangeHandle?.Dispose();
            OnGrpcChangeHandle?.Dispose();
            OnHostChangeHandle?.Dispose();
            OnRemotesChangeHandle?.Dispose();
        }

        public void Enable()
        {
            OnCertificateChangeHandle = _certificateMonitor.OnChange(OnOptionsChangeRestart);
            OnGrpcChangeHandle = _grpcMonitor.OnChange(OnOptionsChangeRestart);
            OnHostChangeHandle = _hostMonitor.OnChange(OnOptionsChangeRestart);
            OnRemotesChangeHandle = _remotesMonitor.OnChange(OnOptionsChangeRestart);
        }

        public void OnOptionsChangeRestart(object _)
        {
            lock (_restartLock)
            {
                if (!_restarting)
                {
                    _restarting = true;
                    _logger.LogInformation("Restarting {host} as some appsettings.json changed", _hostMonitor.CurrentValue.Host);
                    _appLifetime.StopApplication();
                }
            }
        }
    }
}