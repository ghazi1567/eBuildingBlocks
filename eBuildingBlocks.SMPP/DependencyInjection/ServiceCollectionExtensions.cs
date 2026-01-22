using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.BackgroundJobs;
using eBuildingBlocks.SMPP.Handlers;
using eBuildingBlocks.SMPP.Reassembly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegistarSMPPCore(this IServiceCollection services)
        {
            // Default policy (consumer can override)
            services.TryAddSingleton<ISmppSessionPolicy, DefaultSmppSessionPolicy>();

            // ======================
            // Core utilities
            // ======================
            services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();
            services.AddSingleton<ISmppMessageReassembler, InMemorySmppMessageReassembler>();
            services.AddSingleton<IBindRegistry, InMemoryBindRegistry>();

            // ======================
            // Bind rules (ORDER MATTERS)
            // ======================

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, ActiveAccountBindRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, InterfaceVersionRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, BindModeRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, SystemTypeRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, IpAllowRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IBindRule, MaxBindRule>());


            // ======================
            // Submit rules (ORDER MATTERS)
            // ======================
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, BoundStateSubmitRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, RateLimitRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, TonNpiRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, AddressPrefixRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, DataCodingRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, ConcatenationRule>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ISubmitRule, RegisteredDeliveryRule>());

            services.AddHostedService<SmppSessionCleanupJob>();
            return services;
        }

    }
}
