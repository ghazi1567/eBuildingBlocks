using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Application.Abstractions
{
    public interface IApiKeyValidator
    {
        Task<ApiKeyValidationResult> ValidateAsync(string apiKey, HttpContext context);
    }

}
