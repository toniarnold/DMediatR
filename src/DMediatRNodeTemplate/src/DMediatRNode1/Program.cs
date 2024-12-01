using DMediatR;
using Microsoft.Extensions.Options;

namespace DMediatRNode
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                cts.Cancel();
                eventArgs.Cancel = true;
            };

            var defaultBuilder = GrpcServer.CreateWebAppBuilder(args);
            var defaultApp = GrpcServer.CreateWebApp(defaultBuilder, GrpcPort.UseDefault);

            var renewBuilder = GrpcServer.CreateWebAppBuilder(args);
            var renewApp = GrpcServer.CreateWebApp(renewBuilder, GrpcPort.UseRenew);

            var ct = cts.Token;

            if (args.Length > 0 && args[0] == "init")
            {
                var certs = defaultApp.Services.GetRequiredService<Certificates>();
                await certs.SetUpInitialChainAsync(ct);
                var opt = defaultApp.Services.GetRequiredService<IOptions<CertificateOptions>>().Value;
                Console.WriteLine($"Generated a valid initial certificate chain in {Path.GetFullPath(opt.FilePath!)}");
            }
            else
            {
                await Task.WhenAny(
                    GrpcServer.RunAutoRestartAsync(defaultApp, ct),
                    GrpcServer.RunAutoRestartAsync(renewApp, ct)
                    );
            }
        }
    }
}