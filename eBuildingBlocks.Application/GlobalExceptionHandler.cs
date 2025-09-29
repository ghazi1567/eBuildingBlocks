using eBuildingBlocks.Application.Exceptions;
using eBuildingBlocks.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace eBuildingBlocks.Application;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) // one catch only
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        // Don’t try to write if headers/body already sent
        if (ctx.Response.HasStarted)
        {
            _logger.LogError(ex, "Unhandled exception after response started. TraceId={TraceId}", ctx.TraceIdentifier);
            return;
        }

        var (status, response) = MapException(ex);

        // Log with full exception + trace id
        _logger.LogError(
            ex,
            "Unhandled exception mapped to {StatusCode}. Path={Path} TraceId={TraceId}",
            (int)status, ctx.Request.Path, ctx.TraceIdentifier
        );

        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json; charset=utf-8";

        var payload = JsonSerializer.Serialize(response, JsonOptions);
        await ctx.Response.WriteAsync(payload);
    }

    /// <summary>
    /// Central mapping of exception -> (status, Response)
    /// Add your domain exceptions here.
    /// </summary>
    private static (HttpStatusCode Status, ResponseModel Response) MapException(Exception ex) =>
        ex switch
        {
            // Domain/application exceptions (examples)
            BadRequestException bre
                => (HttpStatusCode.BadRequest, ResponseModel.Fail(bre.Error ?? "Bad request")),
            UnauthorizedException ue
                => (HttpStatusCode.Unauthorized, ResponseModel.Fail(ue.Error ?? "Unauthorized", HttpStatusCode.Unauthorized)),
            ForbiddenException fe
                => (HttpStatusCode.Forbidden, ResponseModel.Fail(fe.Error ?? "Forbidden", HttpStatusCode.Forbidden)),
            NotFoundException nfe
                => (HttpStatusCode.NotFound, ResponseModel.Fail(nfe.Error ?? "Not found", HttpStatusCode.NotFound)),
            MethodNotAllowedException mna
                => (HttpStatusCode.MethodNotAllowed, ResponseModel.Fail(mna.Error ?? "Method not allowed", HttpStatusCode.MethodNotAllowed)),
            ConflictException ce
                => (HttpStatusCode.Conflict, ResponseModel.Fail(ce.Error ?? "Conflict", HttpStatusCode.Conflict)),
            TooManyRequestException tm
                => (HttpStatusCode.TooManyRequests, ResponseModel.Fail(tm.Error ?? "Too many requests", HttpStatusCode.TooManyRequests)),
            Exceptions.NotImplementedException nie
                => (HttpStatusCode.NotImplemented, ResponseModel.Fail(nie.Error ?? "Not implemented", HttpStatusCode.NotImplemented)),

            // Cancellation: surface 499 or 400; 499 is non-standard—many stick to 400
            OperationCanceledException
                => (HttpStatusCode.BadRequest, ResponseModel.Fail("Request canceled", HttpStatusCode.BadRequest)),

            // Fallback
            _ => (HttpStatusCode.InternalServerError, ResponseModel.Fail("An unexpected error occurred.", HttpStatusCode.InternalServerError))
        };



    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };
}
