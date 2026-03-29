namespace eBuildingBlocks.ReferenceApp.Application.Orders;

public sealed record PlaceOrderResultDto(Guid Id, string OrderNumber, decimal TotalAmount, string Status);

public sealed record OrderDetailsDto(Guid Id, string OrderNumber, decimal TotalAmount, string Status);
