using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Application.Models
{
    public class GridQueryModel
    {
        public int PageIndex { get; init; } = 0;          // 0-based
        public int PageSize { get; init; } = 10;         // 10..100 guard in controller
        public string? SortBy { get; init; }              // e.g. "name" or "category.name"
        public string? SortDir { get; init; }             // "asc" | "desc"
        public string? Search { get; init; }              // free-text search
        public Dictionary<string, string[]>? Filters { get; init; } // column -> values
        public string[]? Status { get; init; }            // optional multi-filter
        public DateOnly? FromDate { get; init; }          // optional
        public DateOnly? ToDate { get; init; }          // optional
    }
}
