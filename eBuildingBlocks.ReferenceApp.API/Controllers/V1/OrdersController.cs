using Asp.Versioning;
using eBuildingBlocks.API.Controllers;
using eBuildingBlocks.Application.Features;
using eBuildingBlocks.ReferenceApp.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace eBuildingBlocks.ReferenceApp.API.Controllers.V1;

/// <summary>
/// Versioned surface mapped under <c>api/v1/orders</c> via Asp.Versioning + route token substitution.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class OrdersController(
    PlaceOrderCommandHandler placeOrderHandler,
    GetOrderByIdQueryHandler getOrderByIdHandler) : BaseController()
{
    /// <summary>
    /// Creates an order; domain events are written to the transactional outbox on SaveChanges (see handler comments).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResponseModel<PlaceOrderResultDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await placeOrderHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
        return ApiResult(result);
    }

    /// <summary>Loads a single order by id, enforcing tenant isolation when multi-tenancy is enabled.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResponseModel<OrderDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getOrderByIdHandler.HandleAsync(id, cancellationToken).ConfigureAwait(false);
        return ApiResult(result);
    }
}
