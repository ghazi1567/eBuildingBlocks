using eBuildingBlocks.Application.Features;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace eBuildingBlocks.Application.Middlewares;

/// <summary>
/// Normalizes selected HTTP status codes to a JSON <see cref="ResponseModel"/> when the response has not started
/// and no body was committed (avoids throwing after the pipeline, which can corrupt responses).
/// </summary>
public sealed class HttpResponseMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public async Task Invoke(HttpContext context)
    {
        await next(context).ConfigureAwait(false);

        if (context.Response.HasStarted)
            return;

        var payload = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => ResponseModel.Fail(
                "Unauthorized. Access is denied.", HttpStatusCode.Unauthorized),
            StatusCodes.Status403Forbidden => ResponseModel.Fail(
                "Forbidden. Access is denied.", HttpStatusCode.Forbidden),
            StatusCodes.Status405MethodNotAllowed => ResponseModel.Fail(
                "Method not allowed.", HttpStatusCode.MethodNotAllowed),
            _ => (ResponseModel?)null
        };

        if (payload is null)
            return;

        TryResetResponseBody(context);

        context.Response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }

    private static void TryResetResponseBody(HttpContext context)
    {
        try
        {
            if (context.Response.Body.CanSeek)
            {
                context.Response.Body.SetLength(0);
                context.Response.Body.Position = 0;
            }
        }
        catch
        {
            // Non-seekable stream; leave body as-is and skip rewrite (HasStarted often true anyway)
        }
    }
}
