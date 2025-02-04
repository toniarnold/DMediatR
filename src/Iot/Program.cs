using System.Reflection;

namespace Iot
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
                var certPath = GrpcServer.SetUpInitialCertificateChain(loggingBuilder => loggingBuilder.AddConsole());
                Console.WriteLine($"Created the initial certificate chain in {certPath}");
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
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseDefault, AddServices, ct),
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseRenew, AddServices, ct)
                    );
            }
        }

        private static void AddServices(WebApplicationBuilder builder)
        {
            builder.Logging.AddConsole();
            builder.Services.AddDMediatR(builder.Configuration,
                cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        }
    }
}