using Asp.Versioning;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Implementations;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text.Json.Serialization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.API.Startup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection BaseRegister(this IServiceCollection services,
        IConfiguration configuration, IHostBuilder hostBuilder)
    {
        services
            .RegisterControllers()
            .RegisterAPIVersioning()
            .RegisterLog(configuration)
            .RegisterMemoryCache()
            .RegisterRedis(configuration)
            .RegisterCurrentUser()
            .RegisterSwagger()
            .RegisterCors()
            .RegisterHangfire();

        return services;
    }

    private static IServiceCollection RegisterControllers(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();

        return services;
    }

    public static IServiceCollection RegisterAPIVersioning(this IServiceCollection services)
    {
        services

            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
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

    public static IServiceCollection RegisterLog(this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = string.IsNullOrEmpty(configuration["OpenTelemetry:name"])? "": configuration["OpenTelemetry:name"];
        var url = string.IsNullOrEmpty(configuration["OpenTelemetry:url"]) ? "" : configuration["OpenTelemetry:url"];

        if (string.IsNullOrEmpty(url))
        {
            return services;
        }
        if (string.IsNullOrEmpty(serviceName))
            serviceName = "DefaultApiService";

        services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName))  // <-- use your service name
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(url);
                });
        });

        return services;
    }

    public static IServiceCollection RegisterMemoryCache(this IServiceCollection services)
    {
        services.AddMemoryCache();

        return services;
    }

    public static IServiceCollection RegisterRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var cfg = configuration.GetConnectionString("RedisConnection");
            return ConnectionMultiplexer.Connect(cfg!);
        });

        return services;
    }


    private static IServiceCollection RegisterCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentUser, CurrentUser>();

        return services;
    }

    private static IServiceCollection RegisterSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Project Swagger",
                Version = "v1",
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
            // 👇 Add this to apply the security to all endpoints
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2", // can be any string
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>()
                }
            });
        });

        return services;
    }

    private static IServiceCollection RegisterCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("allowall", policy =>
            {
                policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        });

        return services;
    }

  

   

    public static IServiceCollection RegisterFeatureManagement(this IServiceCollection services)
    {
        services.AddFeatureManagement();

        return services;
    }

    public static IServiceCollection RegisterHangfire(this IServiceCollection services)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseDefaultTypeSerializer()
            .UseMemoryStorage());

        services.AddHangfireServer();

        return services;
    }
}