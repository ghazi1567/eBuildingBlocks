using eBuildingBlocks.Domain.Models;

namespace eBlocksApi.Domain.Products;

public class Product : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // EF Core materialization constructor.
    private Product()
    {
    }

    public static Product Create(string name, decimal price)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name));
        return product;
    }
}
