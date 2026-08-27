using System.Net;
using eBlocksApi.Application.Products;
using eBlocksApi.Domain.Products;
using eBuildingBlocks.API.Controllers;
using eBuildingBlocks.Application.Features;
using eBuildingBlocks.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eBlocksApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IRepository<Product, Guid> products, IUnitOfWork unitOfWork) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await products.ListAllAsync(cancellationToken);
        var dtos = items.Select(ProductDto.FromEntity).ToList();
        return ApiResult(ResponseModel<List<ProductDto>>.Ok(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return ApiResult(ResponseModel<ProductDto>.Fail("Product not found.", HttpStatusCode.NotFound));

        return ApiResult(ResponseModel<ProductDto>.Ok(ProductDto.FromEntity(product)));
    }

    public record CreateProductRequest(string Name, decimal Price);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Price);

        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResult(ResponseModel<ProductDto>.Created(ProductDto.FromEntity(product)));
    }
}
