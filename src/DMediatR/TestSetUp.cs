using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.RegularExpressions;

namespace DMediatR
{
    /// <summary>
    /// Helper methods for setting up a DMediatR environment in functional unit
    /// tests without a dependency to a specific test framework.
    /// </summary>
    public static class TestSetUp
    {
        #region DMediatR gRPC server

        public const int ServerStartTimeout = 1;
        public static List<Process> ServerProcesses { get; private set; } = [];
        public static ServiceProvider ServiceProvider { get; private set; } = default!;

        public static CertificateOptions CertificateOptions =>
            ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection(CertificateOptions.SectionName).Get<CertificateOptions>()!;

        /// <summary>
        /// Start the gRPC server project with the launch profile.
        /// </summary>
        /// <param name="workingDirectory">The server project directory.</param>
        /// <param name="serverProject">The .csproj file of the server project.</param>
        /// <param name="launchProfile">Defined in its Properties\launchSettings.json.</param>
        /// <param name="port">The main port to wait for accepting TCP connections.</param>
        /// <param name="oldPort">he TCP renewal port to wait for accepting TCP connections.</param>
        public static void StartServer(string workingDirectory, string serverProject, string launchProfile, int port, int oldPort)
        {
            var info = new ProcessStartInfo();
            info.FileName = Path.Join(System.Environment.GetEnvironmentVariable("ProgramFiles"), "dotnet", "dotnet.exe");
            info.Arguments = $"run --no-build --project {serverProject} --launch-profile {launchProfile}";
            info.WorkingDirectory = workingDirectory;
            info.UseShellExecute = true;
            ServerProcesses.Add(Process.Start(info)!);
            WaitForServerPort(port, ServerStartTimeout);
            WaitForServerPort(oldPort, ServerStartTimeout);
        }

        public static void AssertServersStarted(Action<Process, string> processAssert)
        {
            foreach (var process in ServerProcesses)
            {
                var profile = GetProcessProfile(process);
                processAssert(process, profile);
            }
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

        #endregion DMediatR gRPC server

        #region ServiceProvider

        /// <summary>
        /// Instantiate the ServiceProvider with appsettings[.environment].json
        /// </summary>
        /// <param name="environment"></param>
        public static void SetUpDMediatRServices(string? environment,
            Action<MediatRServiceConfiguration> mediatrCfg,
            Func<IServiceCollection, IServiceCollection>? serviceCollectionAction = null)
        {
            ServiceCollection sc = new();
            var cfg = GetConfiguration(environment ?? "");
            sc.AddDMediatR(cfg, mediatrCfg);
            serviceCollectionAction?.Invoke(sc);
            ServiceProvider = sc.BuildServiceProvider();
            // Implicitly sets static properties as MessagePackCompression
            var _ = ServiceProvider.GetRequiredService<IOptions<GrpcOptions>>().Value;
        }

        public static IConfiguration GetConfiguration(string environment)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();
            return builder.Build();
        }

        #endregion ServiceProvider

        #region HTTPS

        /// <summary>
        /// Create an initial valid X509 certificate chain in the directory
        /// configured in the Certificate section.
        /// </summary>
        public static void SetUpInitialCertificates()
        {
            var certs = ServiceProvider.GetRequiredService<Certificates>();
            Task.Run(() => certs.SetUpInitialChainAsync(CancellationToken.None)).Wait();
        }

        /// <summary>
        /// Side-effect free HTTPS client using the client certificate.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async static Task<HttpClient> GetHttpClientAsync()
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                // Required for linux-arm64 server:
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
            };
            var clientCertificateProvider = ServiceProvider.GetRequiredService<ClientCertificateProvider>();
            (var loaded, var cert) = await clientCertificateProvider.TryLoad(CancellationToken.None);
            if (!loaded)
            {
                throw new Exception($"Client certificate {clientCertificateProvider.FileNamePfx} not found");
            }
            handler.ClientCertificates.Add(cert!);
            return new HttpClient(handler)
            {
                DefaultRequestVersion = HttpVersion.Version20 // HTTP/2 required for gRPC
            };
        }

        public static void DeployCertificate(string certificate, string node)
        {
            var path = CertificateOptions.FilePath!;

            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, certificate), true);
            var oldCertificate = Regex.Replace(certificate, @".(\w\w\w)$", @"-old.$1");
            File.Copy(Path.Combine(path, certificate), Path.Combine(path, node, oldCertificate), true);
        }

        #endregion HTTPS
    }
}