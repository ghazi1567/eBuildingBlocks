using eBuildingBlocks.Application.Features;
using Microsoft.AspNetCore.Mvc;

namespace eBuildingBlocks.API.Controllers;

[ApiController]
public abstract class BaseController() : ControllerBase
{
    // TODO: Add base controller methods
    protected IActionResult ApiResult(ResponseModel result)
    {
        if (result.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    protected IActionResult ApiResult<T>(ResponseModel<T> result)
    {
        if (result.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

}
