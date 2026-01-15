using eBuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace eBuildingBlocks.Application.Middlewares
{
    public sealed class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            // Skip if already authenticated (JWT, etc.)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Try resolve validator (OPTIONAL)
            var validator = context.RequestServices.GetService<IApiKeyValidator>();
            if (validator == null)
            {
                await _next(context); // API key auth not configured
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-API-KEY", out var apiKey))
            {
                await _next(context);
                return;
            }

            var result = await validator.ValidateAsync(apiKey!, context);

            if (!result.IsValid)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            var claims = result.Claims ?? Enumerable.Empty<Claim>();

            var identity = new ClaimsIdentity(claims, "ApiKey");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }
    }

}
