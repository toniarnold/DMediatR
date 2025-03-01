﻿using CertificateManager;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc.Server;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace DMediatR
{
    /// <summary>
    /// Port resp. SSL certificate to use.
    /// </summary>
    public enum GrpcPort
    {
        /// <summary>
        /// Use the configured default port with the  current certificate.
        /// </summary>
        UseDefault,

        /// <summary>
        /// Use the configured OldPort with the old certificate.
        /// </summary>
        UseRenew
    }

    /// <summary>
    /// Utility for creating a gRPC service WebApplicationBuilder and WebApplication in a DMediatR node.
    /// </summary>
    public static class GrpcServer
    {
        private static X509Certificate2? _intemediateCrt;
        private static X509Certificate2? _intemediateCrtOld;
        private static ILogger<X509Chain>? _logger;

        /// <summary>
        /// The intermediate certificate client and server certificates are
        /// validated against.
        /// </summary>
        public static X509Certificate2? IntermediateCrt => _intemediateCrt;

        /// <summary>
        /// The old intermediate certificate client and server certificates were
        /// validated against. Used for certificate renewal
        /// </summary>
        public static X509Certificate2? IntermediateCrtOld => _intemediateCrtOld;

        public static ILogger<X509Chain>? Logger => _logger;

        /// <summary>
        /// For setting the Console.Title in Program.cs. Includes the optional
        /// ASPNETCORE_ENVIRONMENT, Host and Port.
        /// </summary>
        public static string ConsoleTitle
        {
            get
            {
                var env = Environment.GetEnvironmentVariables();
                if (env.Contains("ASPNETCORE_ENVIRONMENT")) // dotnet run --project
                {
                    var environment = (string)env["ASPNETCORE_ENVIRONMENT"]!;
                    var opt = GetHostOptions(environment);
                    return $"DMediatR {environment} on {opt.Host}:{opt.Port}";
                }
                else
                {
                    var opt = GetHostOptions();
                    return $"DMediatR on {opt.Host}:{opt.Port}";
                }
            }
        }

        /// <summary>
        /// Generate or renew the certificate chain by directly using the local services.
        /// </summary>
        /// <returns>FilePath where the certificate chain was saved.</returns>
        public static string SetUpInitialCertificateChain(Action<ILoggingBuilder> configureLogging)
        {
            var env = Environment.GetEnvironmentVariables();
            var environment = env.Contains("ASPNETCORE_ENVIRONMENT") ? env["ASPNETCORE_ENVIRONMENT"] : "";
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();
            var cfg = builder.Build();
            ServiceCollection sc = new();
            sc.AddDMediatR(cfg);
            sc.AddLogging(configureLogging);
            var sp = sc.BuildServiceProvider();
            var certs = sp.GetRequiredService<Certificates>();
            Task.Run(() => certs.SetUpInitialChainAsync(CancellationToken.None)).Wait();
            var opt = sp.GetRequiredService<IOptions<CertificateOptions>>().Value;
            return opt.FilePath!;
        }

        public static WebApplicationBuilder CreateWebAppBuilder(string[] args, GrpcPort usePort = GrpcPort.UseDefault)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddDMediatR(builder.Configuration);

            // Grpc
            var sp = builder.Services.BuildServiceProvider();
            var grpcOptions = sp.GetRequiredService<IOptions<GrpcOptions>>().Value; // implicitly sets the static properties
            builder.Services.AddCodeFirstGrpc(grpcOptions.AssignOptions);

            builder.Services.AddAuthentication(
                CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate()
                .AddCertificateCache();
            var serverCertificate =
                    usePort == GrpcPort.UseRenew ? ServerCertificateProvider.LoadCertificateOld(builder.Configuration)
                                    : ServerCertificateProvider.LoadCertificate(builder.Configuration);
            var certificateCollection = new X509Certificate2Collection(serverCertificate!);
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(listenOptions =>
                {
                    listenOptions.CheckCertificateRevocation = false;
                    listenOptions.ServerCertificateChain = certificateCollection;
                    listenOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    listenOptions.ClientCertificateValidation =
                        usePort == GrpcPort.UseRenew ? IsClientCertificateValidOld : IsClientCertificateValid;
                });
                if (grpcOptions.MaxMessageSize != null)
                {
                    serverOptions.Limits.Http2.InitialConnectionWindowSize = (int)grpcOptions.MaxMessageSize;
                }
            });
            builder.Services.AddHostedService<ServerCertificateOptionsWatcher>();

            return builder;
        }

        private static bool IsClientCertificateValid(X509Certificate2 cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return IsClientCertificateValid(false, cert, chain, sslErrors);
        }

        private static bool IsClientCertificateValidOld(X509Certificate2 cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return IsClientCertificateValid(true, cert, chain, sslErrors);
        }

        private static bool IsClientCertificateValid(bool old, X509Certificate2 cert, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            // A linux-arm64 server rejects if (sslErrors != SslPolicyErrors.None)
            bool valid = false;
            var policy = new X509ChainPolicy
            {
                RevocationFlag = X509RevocationFlag.ExcludeRoot,
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority |
                    (old ? X509VerificationFlags.IgnoreCtlNotTimeValid : 0)
            };
            policy.ApplicationPolicy.Add(OidLookup.ClientAuthentication);
            policy.ExtraStore.Add(old ? _intemediateCrtOld! : _intemediateCrt!);
            policy.ExtraStore.Add(cert);
            if (chain != null)
            {
                chain.ChainPolicy = policy;
                valid = chain.Build(cert);

                if (!valid)
                {
                    var status = from s in chain.ChainStatus select $"{s.Status}: {s.StatusInformation}";
                    string failures = System.String.Join("\n          ", status);
                    _logger!.LogWarning("Client certificate custom validation failure\n          {failures}", failures);
                }
            }

            return valid;
        }

        public static WebApplication CreateWebApp(WebApplicationBuilder builder, GrpcPort usePort = GrpcPort.UseDefault)
        {
            var app = builder.Build();
            app.UseAuthentication();
            app.MapGrpcService<DtoService>();
            var hostOptions = app.Services.GetRequiredService<IOptions<HostOptions>>().Value;
            hostOptions.GrpcPort = usePort;
            switch (usePort)
            {
                default:
                case GrpcPort.UseDefault:
                    app.Urls.Add(hostOptions.Address);
                    break;

                case GrpcPort.UseRenew:
                    app.Urls.Add(hostOptions.OldAddress);
                    break;
            }
            _logger = app.Services.GetRequiredService<ILogger<X509Chain>>();
            LoadIntermediateCertificates(app.Services); // for IsClientCertificateValid
            var grpcOptions = app.Services.GetRequiredService<IOptions<GrpcOptions>>().Value;
            if (grpcOptions.RemotesSvg)
            {
                app.MapGet("/remotes.svg", context => GetRemotesSvg(app.Services, context));
            }
            app.MapGet("/", () => ConsoleTitle);
            return app;
        }

        private static async Task GetRemotesSvg(IServiceProvider sp, HttpContext context)
        {
            var remotesGraph = sp.GetRequiredService<IRemotesGraph>();
            var svg = await remotesGraph.GetSvgAsync();
            context.Response.Clear();
            context.Response.ContentType = MediaTypeNames.Image.Svg;
            await context.Response.WriteAsync(svg);
        }

        /// <summary>
        /// Build and run the WebApplication, repeat after it was stopped with
        /// IHostApplicationLifetime.StopApplication().
        /// </summary>
        /// <param name="args"></param>
        /// <param name="usePort"></param>
        /// <param name="builderCfg">Optional callback for additional
        /// configuration of the WebApplicationBuilder, such as adding a
        /// concrete logger.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task RunRestartWebAppAsync(string[] args, GrpcPort usePort,
            Action<WebApplicationBuilder>? builderCfg, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var builder = CreateWebAppBuilder(args);
                builderCfg?.Invoke(builder);
                using var app = CreateWebApp(builder, usePort);
                try
                {
                    await app.RunAsync(cancellationToken);
                }
                catch (OperationCanceledException) { } // after StopApplication()
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get the Host options outside of the WebApplicationBuilder.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static HostOptions GetHostOptions(string environment = "")
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true);
            var config = builder.Build();
            var opts = new HostOptions();
            config.GetSection(HostOptions.SectionName).Bind(opts);
            return opts;
        }

        /// <summary>
        /// Used only for validation, thus take the .crt file and ignore
        /// RenewBeforeExpirationDays.
        /// </summary>
        /// <param name="usePort"></param>
        /// <param name="serviceProvider"></param>
        /// <exception cref="Exception"></exception>
        public static void LoadIntermediateCertificates(IServiceProvider serviceProvider)
        {
            var intermediateCertProvider = serviceProvider.GetRequiredService<IntermediateCertificateProvider>();
            (var loaded, _intemediateCrt) = Task.Run<(bool, X509Certificate2?)>(
                () => intermediateCertProvider.TryLoadCrt(GrpcPort.UseDefault, CancellationToken.None)).Result;
            if (!loaded)
            {
                throw new Exception($"Validation certificate {intermediateCertProvider.FileNameCrt} not found");
            }
            (loaded, _intemediateCrtOld) = Task.Run<(bool, X509Certificate2?)>(
                () => intermediateCertProvider.TryLoadCrt(GrpcPort.UseRenew, CancellationToken.None)).Result;
            if (!loaded)
            {
                throw new Exception($"Validation certificate {intermediateCertProvider.FileNameOldCrt} not found");
            }
        }
    }
}