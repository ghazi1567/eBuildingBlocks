using eBuildingBlocks.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace eBuildingBlocks.API.Controllers;

[ApiController]
public abstract class BaseController() : ControllerBase
{
    /// <summary>Maps <see cref="ResponseModel.StatusCode"/> to an <see cref="IActionResult"/>.</summary>
    protected IActionResult ApiResult(ResponseModel result) => MapResponse(result);

    /// <summary>Maps <see cref="ResponseModel.StatusCode"/> to an <see cref="IActionResult"/>.</summary>
    protected IActionResult ApiResult<T>(ResponseModel<T> result) => MapResponse(result);

    private static IActionResult MapResponse(ResponseModel result)
    {
        if (result.StatusCode == HttpStatusCode.NoContent)
            return new NoContentResult();

        return result.StatusCode switch
        {
            HttpStatusCode.OK => new OkObjectResult(result),
            HttpStatusCode.Created => new ObjectResult(result) { StatusCode = StatusCodes.Status201Created },
            HttpStatusCode.Accepted => new ObjectResult(result) { StatusCode = StatusCodes.Status202Accepted },
            HttpStatusCode.BadRequest => new BadRequestObjectResult(result),
            HttpStatusCode.Unauthorized => new ObjectResult(result) { StatusCode = StatusCodes.Status401Unauthorized },
            HttpStatusCode.Forbidden => new ObjectResult(result) { StatusCode = StatusCodes.Status403Forbidden },
            HttpStatusCode.NotFound => new NotFoundObjectResult(result),
            HttpStatusCode.Conflict => new ObjectResult(result) { StatusCode = StatusCodes.Status409Conflict },
            HttpStatusCode.MethodNotAllowed => new ObjectResult(result) { StatusCode = StatusCodes.Status405MethodNotAllowed },
            HttpStatusCode.TooManyRequests => new ObjectResult(result) { StatusCode = StatusCodes.Status429TooManyRequests },
            HttpStatusCode.NotImplemented => new ObjectResult(result) { StatusCode = StatusCodes.Status501NotImplemented },
            HttpStatusCode.RequestTimeout => new ObjectResult(result) { StatusCode = StatusCodes.Status408RequestTimeout },
            _ => new ObjectResult(result) { StatusCode = (int)result.StatusCode }
        };
    }
}
