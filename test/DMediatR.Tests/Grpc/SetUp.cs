using DMediatRNode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

namespace DMediatR.Tests.Grpc
{
    /// <summary>
    /// Project- and NUnit specific TestSetUp methods.
    /// </summary>
    public static class SetUp
    {
        public const string GrpcServerProject = "DMediatRNode.csproj";
        public const string WorkDirFromTestDir = @"..\..\..\..\..\src\DMediatRNode";
        public const string OutputDirFromTestDir = @"..\..\..\output";
        public const int ServerStartTimeout = 1;
        public static ServiceProvider ServiceProvider => TestSetUp.ServiceProvider;
        public static CertificateOptions CertificateOptions => TestSetUp.CertificateOptions;

        /// <summary>
        /// Start the gRPC server project with the working directory determined here.
        /// </summary>
        /// <param name="launchProfile"></param>
        /// <param name="port"></param>
        /// <param name="oldPort"></param>
        public static void StartServer(string launchProfile, int port, int oldPort)
        {
            var workDir = Path.GetFullPath(Path.Join(TestContext.CurrentContext.WorkDirectory, WorkDirFromTestDir));
            TestSetUp.StartServer(workDir, GrpcServerProject, launchProfile, port, oldPort);
        }

        public static void AssertServersStarted()
        {
            Assert.Multiple(() =>
            {
                TestSetUp.AssertServersStarted(ProcessAssert);
            });
        }

        private static void ProcessAssert(Process process, string profile)
        {
            Assert.That(process.HasExited, Is.False, $"Process {profile} has exited");
        }

        public static void StopAllServers()
        {
            TestSetUp.StopAllServers();
        }

        public static void SetUpInitialCertificates()
        {
            TestSetUp.SetUpInitialCertificates();
        }

        public static void DeployCertificate(string certificate, string node)
        {
            TestSetUp.DeployCertificate(certificate, node);
        }

        /// <summary>
        /// Instantiate the ServiceProvider with appsettings[.environment].json
        /// and Extensions.Logging.NUnit
        /// </summary>
        /// <param name="environment"></param>
        public static void SetUpDMediatRServices(string? environment = null)
        {
            TestSetUp.SetUpDMediatRServices(environment,
                cfg => cfg.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(Program))!),
                sc => sc.AddLogging(builder => builder.AddNUnit()));
        }

        public static void SaveOutput(string filename, string content)
        {
            var outputDir = Path.GetFullPath(Path.Join(TestContext.CurrentContext.WorkDirectory, OutputDirFromTestDir));
            var outputFile = Path.Join(outputDir, filename);
            File.WriteAllText(outputFile, content);
        }
    }
}