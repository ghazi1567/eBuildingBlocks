using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Specifications
{
    public sealed class FieldFilterSpec<T> : Specification<T>
    {
        public FieldFilterSpec(string propertyPath, ComparisonOperator op, object? value, bool track = false, bool ignoreFilters = false)
        {
            Criteria = DynamicPredicate.Build<T>(propertyPath, op, value);
            if (track) WithTracking();
            if (ignoreFilters) WithIgnoreQueryFilters();
        }
    }
}
