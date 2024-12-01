using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc.Server;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    /// <summary>
    /// Port resp. SSL certificate to use
    /// </summary>
    public enum GrpcPort
    {
        /// <summary>
        /// Use the configured default port with the  current certificate
        /// </summary>
        UseDefault,

        /// <summary>
        /// Use the configured OldPort with the old certificate
        /// </summary>
        UseRenew
    }

    public static class GrpcServer
    {
        public static WebApplicationBuilder CreateWebAppBuilder(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddDMediatR(builder.Configuration);

            // Grpc
            builder.Services.AddCodeFirstGrpc();

            builder.Services.AddAuthentication(
                CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate()
                .AddCertificateCache();
            var serverCertificate = ServerCertificateProvider.LoadCertificate(builder.Configuration);
            var certificateCollection = new X509Certificate2Collection(serverCertificate!);
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(listenOptions =>
                {
                    listenOptions.AllowAnyClientCertificate();
                    listenOptions.ServerCertificateChain = certificateCollection;
                    listenOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                });
            });
            builder.Services.AddHostedService<ServerCertificateFileWatcher>();

            return builder;
        }

        public static WebApplication CreateWebApp(WebApplicationBuilder builder, GrpcPort usePort = GrpcPort.UseDefault)
        {
            var app = builder.Build();
            app.UseAuthentication();
            app.MapGrpcService<DtoService>();
            var options = app.Services.GetRequiredService<IOptions<HostOptions>>().Value;
            options.GrpcPort = usePort;
            switch (usePort)
            {
                case GrpcPort.UseDefault:
                    app.Urls.Add($"https://{options.Host}:{options.Port}/");
                    break;

                case GrpcPort.UseRenew:
                    app.Urls.Add($"https://{options.Host}:{options.OldPort}/");
                    break;
            }
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            return app;
        }

        /// <summary>
        /// Build and run the WebApplication, repeat after it was stopped with IHostApplicationLifetime.StopApplication()
        /// </summary>
        /// <param name="args"></param>
        /// <param name="usePort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task RunRestartWebAppAsync(string[] args, GrpcPort usePort, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var builder = CreateWebAppBuilder(args);
                using var app = CreateWebApp(builder, usePort);
                try
                {
                    await app.RunAsync(cancellationToken);
                }
                catch (OperationCanceledException) { } // after StopApplication()
            }
            await Task.CompletedTask;
        }
    }
}