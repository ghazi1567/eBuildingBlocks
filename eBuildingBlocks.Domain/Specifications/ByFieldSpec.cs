using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Specifications
{
    public sealed class ByFieldSpec<T> : Specification<T>
    {
        public ByFieldSpec(string fieldName, object value, bool track = true)
        {
            Criteria = DynamicPredicate.Build<T>(fieldName, ComparisonOperator.Equal, value);
            if (track) WithTracking();
        }
    }
}
