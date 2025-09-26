using Microsoft.Extensions.Configuration;

namespace eBuildingBlocks.API.Features
{
    public static class FeatureGate
    {
        public static bool Enabled(IConfiguration cfg, string featurePath, bool fallback = false)
            => cfg.GetValue<bool?>($"{featurePath}:Enabled") ?? fallback;

        public static T? Get<T>(IConfiguration cfg, string path) => cfg.GetSection(path).Get<T>();
    }

}
