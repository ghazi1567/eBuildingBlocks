using System;

namespace eBuildingBlocks.Common.utils
{
    public static class GuidGenerator
    {
        public static Guid NewV7() => Guid.CreateVersion7();
    }
}
