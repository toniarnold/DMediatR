﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace DMediatR
{
    /// <summary>
    /// Provides services.AddDMediatR() as a drop-in replacement for AddMediatR().
    /// </summary>
    public static class ServiceCollectionExtension
    {
        internal static IServiceCollection AddDMediatR(this IServiceCollection services, IConfiguration config)
        {
            return AddDMediatR(services, config, (_) => { });
        }

        /// <summary>
        /// A drop-in replacement for MediatR.AddMediatR adding services for distribution.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="mediatrCfg"></param>
        /// <returns></returns>
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
            services.Configure<PasswordOptions>(config.GetSection(PasswordOptions.SectionName));
            services.TryAddSingleton<IValidateOptions<PasswordOptions>, ValidatePasswordOptions>();
            services.Configure<RemotesOptions>(config.GetSection(RemotesOptions.SectionName));
            services.TryAddSingleton<IValidateOptions<RemotesOptions>, ValidateRemotesOptions>();
            services.Configure<GrpcOptions>(config.GetSection(GrpcOptions.SectionName));
            services.TryAddSingleton<OptionsMonitor>();

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

            services.TryAddSingleton<Remote>();

            // DMediatR
            services.TryAddSingleton<IGrpcChannelPool, GrpcChannelPool>();
            services.AddMemoryCache();
            services.TryAddSingleton<NotificationForwarder>();
            services.TryAddSingleton<PingHandler>();
            services.TryAddSingleton<PingHandlerRemote>();
            services.TryAddSingleton<BingHandler>();
            services.TryAddSingleton<IRemotesGraph, RemotesGraph>();
            services.TryAddSingleton<RemotesGraphHandler>();

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
            // <registerserializers>
            services.TryAddSingleton<ISerializer, Serializer>();
            services.TryAddSingleton<TypedSerializer>();
            services.TryAddKeyedSingleton<ISerializer, BinarySerializer>(typeof(object)); // recursion base case for TypedSerializer
            services.TryAddKeyedSingleton<ISerializer, SerializationCountSerializer>(SerializationCountSerializer.Type);
            services.TryAddKeyedSingleton<ISerializer, X509CertificateSerializer>(X509CertificateSerializer.Type);
            services.TryAddKeyedSingleton<ISerializedInterface, ILockISerializedInterface>(typeof(ILock));
            // </registerserializers>

            return services;
        }
    }
}