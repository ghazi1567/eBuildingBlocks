using eBuildingBlocks.Application;
using eBuildingBlocks.Application.Middlewares;
using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
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
            .UsingCors()
            .UsingSwagger(configuration)
            .UsingHangfire()
            .UsingMetrics()
            .UsingRouting()
            .UsingAuthorization()
            .UsingMiddlewares()
            .UsingEndpoints()
            .UsingSerilog();

        return app;
    }

    public static IApplicationBuilder UsingMiddlewares(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionHandler>();
        app.UseMiddleware<HttpResponseMiddleware>();

        return app;
    }

    public static IApplicationBuilder UsingCors(this IApplicationBuilder app)
    {
        app.UseCors("allowall");

        return app;
    }

    public static IApplicationBuilder UsingSwagger(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                var pathBase = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault();
                pathBase = "";
                if (!string.IsNullOrEmpty(pathBase))
                {
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = pathBase } };
                }
            });
        });

        var pathBase = configuration.GetValue<string>("PathBase");
#if DEBUG
        Console.WriteLine("This code runs only in Debug build.");
#else
                if (string.IsNullOrEmpty(pathBase))
                {
                    throw new ArgumentNullException("PathBase", "PathBase is not configured in appsettings.json or environment variables.");
                }
#endif


        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/v1/swagger.json", "API V1");
        });

        return app;
    }

    public static IApplicationBuilder UsingAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static IApplicationBuilder UsingHangfire(this IApplicationBuilder app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new BasicAuthAuthorizationFilter("admin", "admin") }
        });

        return app;
    }


    public static IApplicationBuilder UsingMetrics(this IApplicationBuilder app)
    {
        app.UseHttpMetrics();

        return app;
    }

    public static IApplicationBuilder UsingRouting(this IApplicationBuilder app)
    {
        app.UseRouting();

        return app;
    }

    public static IApplicationBuilder UsingEndpoints(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/healthz", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
            endpoints.MapMetrics("/metrics");
        });

        return app;
    }

    public static IApplicationBuilder UsingSerilog(this IApplicationBuilder app)
    {
        //app.useseri

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