namespace eBuildingBlocks.ReferenceApp.Application.Orders;

/// <summary>Application use case input for committing a B2B order.</summary>
public sealed record PlaceOrderCommand(string OrderNumber, decimal TotalAmount);
