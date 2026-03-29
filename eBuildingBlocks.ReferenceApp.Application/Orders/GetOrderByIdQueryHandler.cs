using System.Net;
using eBuildingBlocks.Application.Features;
using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.ReferenceApp.Domain.Orders;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.ReferenceApp.Application.Orders;

/// <summary>Read model path using the generic <see cref="IRepository{TEntity,TKey}"/> port.</summary>
public sealed class GetOrderByIdQueryHandler(
    IRepository<Order, Guid> orderRepository,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancyOptions)
{
    public async Task<ResponseModel<OrderDetailsDto>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return ResponseModel<OrderDetailsDto>.Fail("Order not found.", HttpStatusCode.NotFound);

        if (multiTenancyOptions.Value.Enabled && order.TenantId != currentUser.TenantId)
            return ResponseModel<OrderDetailsDto>.Fail("Order not found.", HttpStatusCode.NotFound);

        var dto = new OrderDetailsDto(order.Id, order.OrderNumber, order.TotalAmount, order.Status.ToString());
        return ResponseModel<OrderDetailsDto>.Ok(dto);
    }
}
