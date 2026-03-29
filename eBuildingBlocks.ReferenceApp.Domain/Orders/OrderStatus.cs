namespace eBuildingBlocks.ReferenceApp.Domain.Orders;

/// <summary>B2B order lifecycle states used in the reference domain.</summary>
public enum OrderStatus
{
    /// <summary>Editable cart / quote stage before commitment.</summary>
    Draft = 0,

    /// <summary>Committed order; triggers downstream fulfillment workflows.</summary>
    Placed = 1
}
