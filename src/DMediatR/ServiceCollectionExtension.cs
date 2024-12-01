using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DMediatR
{
    public static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddDMediatR(this IServiceCollection services, IConfiguration config)
        {
            return AddDMediatR(services, config, (_) => { });
        }

        public static IServiceCollection AddDMediatR(this IServiceCollection services, IConfiguration config,
            Action<MediatRServiceConfiguration> mediatrCfg)
        {
            // Use TryAdd as MediatR itself, so any existing registration doesn't get overridden
            services.TryAddSingleton(config);
            services.TryAddSingleton(sp => sp);

            // Options
            services.Configure<HostOptions>(config.GetSection(HostOptions.SectionName));
            services.TryAddSingleton<IValidateOptions<HostOptions>, ValidateHostOptions>();
            services.Configure<CertificateOptions>(config.GetSection(CertificateOptions.SectionName));
            services.TryAddSingleton<IValidateOptions<CertificateOptions>, ValidateCertificateOptions>();
            services.Configure<RemotesOptions>(config.GetSection(RemotesOptions.SectionName));
            services.TryAddSingleton<IValidateOptions<RemotesOptions>, ValidateRemotesOptions>();

            // MediatR
            services.AddMediatR(cfg =>
            {
                // Add external assemblies from the caller
                mediatrCfg(cfg);
                var externalTypeEvaluator = cfg.TypeEvaluator;

                // Add the internal assembly and compose its TypeEvaluator
                cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
                cfg.TypeEvaluator = t => config.SelectLocalRemote(t) && externalTypeEvaluator(t);
            });

            // MediatR NotificationForwarder
            services.TryAddSingleton<RenewNotificationForwarder>();
            services.TryAddSingleton<IGrpcChannelPool, GrpcChannelPool>();
            services.AddMemoryCache();

            // Certificates
            services.AddCertificateManager();
            services.TryAddSingleton<RootCertificateProvider>();
            services.TryAddSingleton<RootCertificateProviderRemote>();
            services.TryAddSingleton<IntermediateCertificateProvider>();
            services.TryAddSingleton<IntermediateCertificateProviderRemote>();
            services.TryAddSingleton<ServerCertificateProvider>();
            services.TryAddSingleton<ServerCertificateProviderRemote>();
            services.TryAddSingleton<ClientCertificateProvider>();
            services.TryAddSingleton<ClientCertificateProviderRemote>();
            services.TryAddSingleton<Certificates>();

            // Serializer default and custom
            services.TryAddSingleton<ISerializer, Serializer>();
            services.TryAddKeyedSingleton<ISerializer, X509CertificateSerializer>(X509CertificateSerializer.Type);

            return services;
        }
    }
}