using eBuildingBlocks.Application.Models;
using System.Net;

namespace eBuildingBlocks.Application.Features;

public record PagedResponseModel<T> : ResponseModel<T>
{
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public int TotalCount { get; set; }
    public new IEnumerable<T> Data { get; set; } = Array.Empty<T>();

    public static PagedResponseModel<T> Create(IReadOnlyList<T> data, int count, GridQueryModel queryModel, string? message = null)
      => new()
      {
          Success = true,
          StatusCode = HttpStatusCode.OK,
          Data = data,
          Message = message,
          Successes = message is null ? [] : [message],
          PageSize = queryModel.PageSize,
          PageNumber = queryModel.PageIndex,
          TotalCount = count
      };
}
