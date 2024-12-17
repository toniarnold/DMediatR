using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace DMediatR.Tests.Grpc
{
    public static class SetUp
    {
        public const string GrpcServerProject = "DMediatRNode.csproj";
        public const string WorkDirFromTestDir = @"..\..\..\..\..\src\DMediatRNode";
        public const int ServerStartTimeout = 1;
        public static ServiceProvider? ServiceProvider { get; private set; }
        public static List<Process> ServerProcesses { get; private set; } = [];

        /// <summary>
        /// Start the server with the launch profile
        /// </summary>
        /// <param name="launchProfile">defined in its Properties\launchSettings.json</param>
        /// <param name="port">The main port to wait for accepting TCP connections.</param>
        /// <param name="oldPort">The TCP renewal port to wait for accepting TCP connections.</param>
        public static void StartServer(string launchProfile, int port, int oldPort)
        {
            var info = new ProcessStartInfo();
            info.FileName = Path.Join(System.Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "dotnet.exe");
            info.Arguments = $"run --no-build --project {GrpcServerProject} --launch-profile {launchProfile}";
            info.WorkingDirectory = Path.GetFullPath(Path.Join(TestContext.CurrentContext.WorkDirectory, WorkDirFromTestDir));
            info.UseShellExecute = true;
            ServerProcesses.Add(Process.Start(info)!);
            WaitForServerPort(port, ServerStartTimeout);
            WaitForServerPort(oldPort, ServerStartTimeout);
        }

        /// <summary>
        /// Extracts the profile name out of a StartInfo string like
        /// FileName = "C:\\Program Files\\dotnet\\dotnet.exe", Arguments = "run --no-build --project DMediatRNode.csproj --launch-profile Monolith", WorkingDirectory = ...
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static string GetProcessProfile(Process process)
        {
            var info = process.StartInfo;
            var profile = Regex.Match(info.Arguments, @"--launch-profile\s+(\w+)");
            return profile.Groups[1].Value;
        }

        /// <summary>
        /// Poll for a successful TCP connection at the given port
        /// </summary>
        /// <param name="port">port to listen on</param>
        /// <param name="timeout">expected duration of all tests in sec</param>
        public static void WaitForServerPort(int port, int timeout = ServerStartTimeout)
        {
            int interval = 1000;    // 1 sec
            int times = timeout;
            bool success = false;
            using (var client = new TcpClient())
            {
                while (!success && times >= 0)
                {
                    try
                    {
                        var asyncResult = client.BeginConnect("localhost", port, null, null);
                        while (!asyncResult.AsyncWaitHandle.WaitOne(interval)) { }
                        client.EndConnect(asyncResult);
                    }
                    catch
                    {
                        timeout--;
                    }
                    success = true;
                }
            }
            if (!success)
            {
                throw new Exception(String.Format("Server on Port {0} not reachable within {1} seconds",
                                                    port, timeout));
            }
        }

        public static void StopAllServers()
        {
            foreach (var process in ServerProcesses)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch { }
                finally
                {
                    process.Dispose();
                }
            }
            ServerProcesses.Clear();
        }

        public static void SetUpInitialCertificates()
        {
            var mediator = ServiceProvider!.GetRequiredService<IMediator>();
            Task.Run(() => mediator.Send(new ServerCertificateRequest())).Wait();
            Task.Run(() => mediator.Send(new ClientCertificateRequest())).Wait();
        }

        /// <summary>
        /// Instantiate the ServiceProvider with appsettings[.environment].json
        /// </summary>
        /// <param name="environment"></param>
        public static void SetUpDMediatRServices(string? environment)
        {
            ServiceCollection cs = new();
            var cfg = Configuration.Get(environment);
            ServiceProvider = cs.AddDMediatR(cfg)
                .BuildServiceProvider();
        }

        /// <summary>
        /// Side-effect free HTTP client using the client certificate.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async static Task<HttpClient> GetHttpClientAsync()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidation
            };
            var clientCertificateProvider = ServiceProvider!.GetRequiredService<ClientCertificateProvider>();
            (var loaded, var cert) = await clientCertificateProvider.TryLoad(CancellationToken.None);
            if (!loaded)
            {
                throw new Exception($"Client certificate {clientCertificateProvider.FileName} not found");
            }
            handler.ClientCertificates.Add(cert!);
            return new HttpClient(handler)
            {
                DefaultRequestVersion = HttpVersion.Version20 // HTTP/2 required for gRPC
            };
        }

        private static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return sslErrors == SslPolicyErrors.None;
        }
    }
}