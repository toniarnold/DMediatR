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
                var certPath = GrpcServer.SetUpInitialCertificateChain(loggingBuilder => loggingBuilder.AddConsole());
                Console.WriteLine($"Created the initial certificate chain in {certPath}");
            }
            else
            {
                Console.Title = GrpcServer.ConsoleTitle;
                await Task.WhenAll(
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseDefault, builder => builder.Logging.AddConsole(), ct),
                    GrpcServer.RunRestartWebAppAsync(args, GrpcPort.UseRenew, builder => builder.Logging.AddConsole(), ct)
                    );
            }
        }
    }
}