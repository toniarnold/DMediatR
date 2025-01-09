using DMediatR;
using Microsoft.Extensions.Options;
using System;

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
            var ct = cts.Token;

            if (args.Length > 0 && args[0] == "init")
            {
                var builder = GrpcServer.CreateWebAppBuilder(args[1..]);
                using var app = GrpcServer.CreateWebApp(builder);
                var certs = app.Services.GetRequiredService<Certificates>();
                await certs.SetUpInitialChainAsync(ct);
                var opt = app.Services.GetRequiredService<IOptions<CertificateOptions>>().Value;
                Console.WriteLine($"Created the initial certificate chain in {Path.GetFullPath(opt.FilePath!)}");
            }
            else
            {
                var env = Environment.GetEnvironmentVariables();
                if (env.Contains("ASPNETCORE_ENVIRONMENT")) // dotnet run --project
                {
                    var environment = (string)env["ASPNETCORE_ENVIRONMENT"]!;
                    var opt = GrpcServer.GetHostOptions(environment);

                    Console.Title = $"DMediatR {environment} on {opt.Host}:{opt.Port}";
                }
                await Task.WhenAll(
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseDefault, AddLogger, ct),
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseRenew, AddLogger, ct)
                    );
            }
        }

        private static void AddLogger(WebApplicationBuilder builder)
        {
            builder.Logging.AddConsole();
        }
    }
}