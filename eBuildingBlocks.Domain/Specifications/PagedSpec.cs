using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Specifications
{
    public sealed class PagedSpec<T> : Specification<T>
    {
        public PagedSpec(int page, int size)
        {
            page = page < 1 ? 1 : page;
            size = size <= 0 ? 20 : size;
            ApplyPaging((page - 1) * size, size);
            // leave AsNoTracking = true for reads
        }
    }
}
