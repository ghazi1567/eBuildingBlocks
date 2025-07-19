using eBuildingBlocks.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace eBuildingBlocks.Application.Middlewares;

public class HttpResponseMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        await next(context);

        switch (context.Response.StatusCode)
        {
            case StatusCodes.Status401Unauthorized: throw new UnauthorizedException("Unauthorized! Access is denied");
            case StatusCodes.Status403Forbidden: throw new ForbiddenException("Forbidden! Access is denied");
            case StatusCodes.Status405MethodNotAllowed: throw new MethodNotAllowedException("Method not allowed");
        }
    }
}
