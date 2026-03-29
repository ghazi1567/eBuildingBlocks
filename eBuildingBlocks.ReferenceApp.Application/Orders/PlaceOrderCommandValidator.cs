using FluentValidation;

namespace eBuildingBlocks.ReferenceApp.Application.Orders;

/// <summary>FluentValidation rules colocated with the command (Application layer boundary).</summary>
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999_999.99m);
    }
}
