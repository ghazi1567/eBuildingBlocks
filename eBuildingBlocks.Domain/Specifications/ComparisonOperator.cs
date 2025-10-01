using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Specifications
{
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,      // string.Contains
        StartsWith,    // string.StartsWith
        EndsWith,      // string.EndsWith
        In             // value ∈ collection (e.g., ["Open","Closed"])
    }

}
