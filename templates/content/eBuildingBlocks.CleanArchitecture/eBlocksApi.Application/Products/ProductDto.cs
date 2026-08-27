using eBlocksApi.Domain.Products;

namespace eBlocksApi.Application.Products;

public record ProductDto(Guid Id, string Name, decimal Price)
{
    public static ProductDto FromEntity(Product product) => new(product.Id, product.Name, product.Price);
}
