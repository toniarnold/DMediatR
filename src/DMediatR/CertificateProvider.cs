using CertificateManager;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    internal abstract class CertificateProvider
    {
        protected readonly HostOptions _hostOptions;
        protected readonly CertificateOptions _certOptions;
        protected readonly RemotesOptions _remotes;
        protected readonly IMediator _mediator;
        protected readonly ISerializer _serializer;
        protected readonly IGrpcChannelPool _grpcChannelProvider;
        protected readonly ImportExportCertificate _importExportCertificate;

        protected CertificateProvider(
                IOptions<HostOptions> hostOptions,
                IOptions<CertificateOptions> certOptions,
                IOptions<RemotesOptions> remotesOptions,
                IMediator mediator,
                ISerializer serializer,
                IGrpcChannelPool channel,
                ImportExportCertificate ioCert)
        {
            _hostOptions = hostOptions.Value;
            _certOptions = certOptions.Value;
            _remotes = remotesOptions.Value;
            _mediator = mediator;
            _serializer = serializer;
            _grpcChannelProvider = channel;
            _importExportCertificate = ioCert;
        }

        public IMediator Mediator => _mediator;
        public ISerializer Serializer => _serializer;
        public IGrpcChannelPool ChannelPool => _grpcChannelProvider;

        public CertificateOptions Options => _certOptions;

        public RemotesOptions Remotes => _remotes;

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
                var cert = new X509Certificate2(bytes, Options.Password);
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