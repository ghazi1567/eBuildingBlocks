using eBuildingBlocks.API.Features;
using eBuildingBlocks.Application;
using eBuildingBlocks.Application.Middlewares;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Prometheus;
using System.Net.Http.Headers;
using System.Text;

namespace BuildingBlocks.API.Startup;

public static class AppUseExtensions
{
    public static IApplicationBuilder BaseAppUse(this IApplicationBuilder app, IConfiguration configuration)
    {
        app
           .UsingCors(configuration)
           .UsingSwagger(configuration)
           .UsingHangfire(configuration)
           .UsingMetrics(configuration)
           .UsingRouting(configuration)
           .UsingAuthorization(configuration)
           .UsingMiddlewares(configuration)
           .UsingEndpoints(configuration)
           .UsingSerilog(configuration);

        return app;
    }

    public static IApplicationBuilder UsingMiddlewares(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Middlewares")) return app;

        if (cfg.GetValue("Features:Middlewares:GlobalException", true))
            app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (cfg.GetValue("Features:Middlewares:HttpResponse", true))
            app.UseMiddleware<HttpResponseMiddleware>();

        return app;
    }

    public static IApplicationBuilder UsingCors(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Cors")) return app;

        var policy = cfg["Features:Cors:Policy"] ?? "allowall";
        app.UseCors(policy);
        return app;
    }

    public static IApplicationBuilder UsingSwagger(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Swagger")) return app;

        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                var pathBase = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault();
                if (!string.IsNullOrEmpty(pathBase))
                    swaggerDoc.Servers = new List<OpenApiServer> { new() { Url = pathBase } };
            });
        });

        var title = cfg["Features:Swagger:Title"] ?? "API";
        var version = cfg["Features:Swagger:Version"] ?? "v1";
        var routePrefix = cfg["Features:Swagger:RoutePrefix"]; // "" for root

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{title} {version}");
            if (routePrefix is not null) options.RoutePrefix = routePrefix; // null = keep default
        });

        return app;
    }
    /// <summary>
    /// This method enables authentication and authorization middlewares. along with API key authentication middleware (optional).
    /// </summary>
    /// <param name="app"></param>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static IApplicationBuilder UsingAuthorization(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Authorization")) return app;

        app.UseAuthentication();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseAuthorization();
        return app;
    }

    public static IApplicationBuilder UsingHangfire(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Hangfire")) return app;

        var path = cfg["Features:Hangfire:DashboardPath"] ?? "/hangfire";
        var user = cfg["Features:Hangfire:User"] ?? "admin";
        var pass = cfg["Features:Hangfire:Password"] ?? "admin";

        app.UseHangfireDashboard(path, new DashboardOptions
        {
            Authorization = new[] { new BasicAuthAuthorizationFilter(user, pass) }
        });

        return app;
    }


    public static IApplicationBuilder UsingMetrics(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Metrics")) return app;

        // Prometheus HTTP request duration/labels etc.
        app.UseHttpMetrics();
        return app;
    }


    public static IApplicationBuilder UsingRouting(this IApplicationBuilder app, IConfiguration cfg)
    {
        // Routing is often required; gate only if you really want to allow disabling
        app.UseRouting();
        return app;
    }

    public static IApplicationBuilder UsingEndpoints(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Endpoints", fallback: true))
            return app;

        app.UseEndpoints(endpoints =>
        {
            if (cfg.GetValue("Features:Endpoints:MapControllers", true))
                endpoints.MapControllers();

            // Health checks
            if (FeatureGate.Enabled(cfg, "Features:Endpoints:HealthChecks"))
            {
                var healthPath = cfg["Features:Endpoints:HealthChecks:Path"] ?? "/healthz";
                endpoints.MapHealthChecks(healthPath, new HealthCheckOptions
                {
                    // Customize as needed:
                    // ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            }

            // Prometheus scrape endpoint
            if (FeatureGate.Enabled(cfg, "Features:Endpoints:Metrics") ||
                FeatureGate.Enabled(cfg, "Features:Metrics")) // allow either section to turn it on
            {
                var metricsPath = cfg["Features:Endpoints:Metrics:Path"] ?? cfg["Features:Metrics:Path"] ?? "/metrics";
                endpoints.MapMetrics(metricsPath);
            }
        });

        return app;
    }

    public static IApplicationBuilder UsingSerilog(this IApplicationBuilder app, IConfiguration cfg)
    {
        if (!FeatureGate.Enabled(cfg, "Features:Serilog")) return app;

        // Serilog request logging middleware if you enabled it in Program.cs builder
        // app.UseSerilogRequestLogging();
        return app;
    }
}

public class BasicAuthAuthorizationFilter(string username, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Expires = "0";

        if (httpContext.Request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = AuthenticationHeaderValue.Parse(httpContext.Request.Headers.Authorization!);

            if (authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter!);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                if (credentials[0] == username && credentials[1] == password)
                {
                    return true;
                }
            }
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire Dashboard\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }
}

public class DashboardNoAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}