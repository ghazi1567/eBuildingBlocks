using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Application.Models
{
    public sealed class ApiKeyValidationResult
    {
        public bool IsValid { get; init; }
        public Guid TenantId { get; init; }
        public Guid KeyId { get; init; }
        public IEnumerable<Claim>? Claims { get; init; }
        public int RateLimitPerMinute { get; set; } = 60;
    }

}
