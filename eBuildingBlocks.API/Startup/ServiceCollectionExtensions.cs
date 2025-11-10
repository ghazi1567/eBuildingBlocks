using Asp.Versioning;
using eBuildingBlocks.API.Features;
using eBuildingBlocks.API.Helpers;
using eBuildingBlocks.Domain.Interfaces;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Text.Json.Serialization;

namespace BuildingBlocks.API.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection BaseRegister(this IServiceCollection services,
        IConfiguration configuration, IHostBuilder hostBuilder)
    {
        // order is explicit; each method no-ops if the feature is disabled
        services
            .RegisterControllers(configuration)
            .RegisterAPIVersioning(configuration)
            .RegisterLog(configuration)
            .RegisterMemoryCache(configuration)
            .RegisterRedis(configuration)
            .RegisterCurrentUser(configuration)
            .RegisterSwagger(configuration)
            .RegisterCors(configuration)
            .RegisterFeatureManagement(configuration)
            .RegisterHangfire(configuration);

        return services;
    }

    private static IServiceCollection RegisterControllers(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Controllers", fallback: true)) return services;

        services
            .AddControllers(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(
                    new SlugifyParameterTransformer()));
            })
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Only add API explorer if Swagger might be used
        if (FeatureGate.Enabled(cfg, "Features:Swagger"))
            services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection RegisterAPIVersioning(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:ApiVersioning")) return services;

        var ver = cfg["Features:ApiVersioning:DefaultVersion"] ?? "1.0";
        var parts = ver.Split('.', 2);
        var major = int.TryParse(parts[0], out var m) ? m : 1;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 0;

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(major, minor);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-API-Version"));
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    public static IServiceCollection RegisterLog(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:OpenTelemetry")) return services;

        var serviceName = cfg["Features:OpenTelemetry:Name"] ?? "DefaultApiService";
        var url = cfg["Features:OpenTelemetry:OtlpUrl"];

        services.AddOpenTelemetry()
        .WithTracing(tp =>
        {
            tp.AddAspNetCoreInstrumentation()
              .AddHttpClientInstrumentation()
              .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

            if (!string.IsNullOrWhiteSpace(url))
            {
                tp.AddOtlpExporter(o => o.Endpoint = new Uri(url));
            }
        });

        return services;
    }

    public static IServiceCollection RegisterMemoryCache(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:MemoryCache", fallback: true)) return services;
        services.AddMemoryCache();
        return services;
    }

    public static IServiceCollection RegisterRedis(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Redis")) return services;

        var conn = cfg.GetConnectionString("RedisConnection");
        if (string.IsNullOrWhiteSpace(conn))
            return services; // silent no-op if not configured

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(conn));
        return services;
    }

    private static IServiceCollection RegisterCurrentUser(this IServiceCollection services, IConfiguration cfg)
    {
        // Usually always on; gate only if you want to optionally remove it
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentUser, TenantResolver>();
        services.AddSingleton<ITenantScope, TenantScope>();
        return services;
    }

    private static IServiceCollection RegisterSwagger(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Swagger")) return services;

        var title = cfg["Features:Swagger:Title"] ?? "Project Swagger";
        var version = cfg["Features:Swagger:Version"] ?? "v1";

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = "",
                Contact = new OpenApiContact
                {
                    Name = "Inam Ul Haq",
                    Email = "inam.sys@gmail.com",
                    Url = new Uri("https://www.linkedin.com/in/inam1567/")
                }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please insert JWT into field",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        });

        return services;
    }

    private static IServiceCollection RegisterCors(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Cors")) return services;

        var policyName = cfg["Features:Cors:Policy"] ?? "allowall";
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        return services;
    }

    public static IServiceCollection RegisterFeatureManagement(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:FeatureManagement")) return services;

        services.AddFeatureManagement();
        return services;
    }

    public static IServiceCollection RegisterHangfire(this IServiceCollection services, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Hangfire")) return services;

        var useMem = cfg.GetValue("Features:Hangfire:UseMemoryStorage", true);

        services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseDefaultTypeSerializer();

            if (useMem) configuration.UseMemoryStorage();
            // else: plug SQL/Redis/etc here based on more config keys
        });

        services.AddHangfireServer();
        return services;
    }
}
