using System.Net;
using eBuildingBlocks.Application.Features;
using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.ReferenceApp.Domain.Orders;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.ReferenceApp.Application.Orders;

/// <summary>
/// Orchestrates persistence for <see cref="PlaceOrderCommand"/>. Uses <see cref="IUnitOfWork.SaveChangesAsync"/>
/// so the same <see cref="Microsoft.EntityFrameworkCore.DbContext"/> that backs the repository runs
/// <c>SaveChanges</c>; the host registers <c>DomainOutboxSaveChangesInterceptor</c> on that context, which
/// turns aggregate domain events into <c>OutboxMessages</c> in the same SQL transaction.
/// </summary>
public sealed class PlaceOrderCommandHandler(
    IRepository<Order, Guid> orderRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancyOptions,
    IValidator<PlaceOrderCommand> validator)
{
    /// <summary>
    /// Validates, mutates a new <see cref="Order"/>, and saves. Domain events become outbox rows automatically
    /// when the infrastructure outbox interceptor is registered on the application's DbContext.
    /// </summary>
    public async Task<ResponseModel<PlaceOrderResultDto>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ResponseModel<PlaceOrderResultDto>.ValidationFail(errors, "Validation failed.");
        }

        var mt = multiTenancyOptions.Value;
        var tenantId = mt.Enabled ? currentUser.TenantId : mt.DefaultTenantId != Guid.Empty ? mt.DefaultTenantId : Guid.Parse("00000000-0000-0000-0000-000000000001");

        if (mt.Enabled && currentUser.TenantId == Guid.Empty)
        {
            return ResponseModel<PlaceOrderResultDto>.Fail(
                "Multi-tenancy is enabled but no tenant was resolved. Send header X-Tenant-Id.",
                HttpStatusCode.BadRequest);
        }

        var order = Order.CreateDraft(tenantId);
        order.PlaceOrder(command.OrderNumber, command.TotalAmount);

        await orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);

        // This triggers the Outbox pattern automatically via DomainOutboxSaveChangesInterceptor on SaveChanges.
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = new PlaceOrderResultDto(
            order.Id,
            order.OrderNumber,
            order.TotalAmount,
            order.Status.ToString());

        return ResponseModel<PlaceOrderResultDto>.Created(dto, "Order placed; domain event written to transactional outbox.");
    }
}
