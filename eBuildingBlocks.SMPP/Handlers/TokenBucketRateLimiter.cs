using eBuildingBlocks.SMPP.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class TokenBucketRateLimiter : IRateLimiter
    {
        private sealed class Bucket
        {
            public double Tokens;
            public long LastTicks;
        }

        private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
        private static readonly double TickFrequency = Stopwatch.Frequency;

        public bool Allow(string key, int? tps, int? mpm)
        {
            if (tps is null && mpm is null) return true;

            // Enforce both if present
            if (tps is not null && !AllowInternal($"{key}:tps", tps.Value, capacity: Math.Max(1, tps.Value * 5))) return false;
            if (mpm is not null && !AllowInternal($"{key}:mpm", mpm.Value / 60.0, capacity: Math.Max(1, mpm.Value))) return false;

            return true;
        }

        private bool AllowInternal(string bucketKey, double ratePerSecond, int capacity)
        {
            var now = Stopwatch.GetTimestamp();
            var bucket = _buckets.GetOrAdd(bucketKey, _ => new Bucket { Tokens = capacity, LastTicks = now });

            lock (bucket)
            {
                var elapsedSeconds = (now - bucket.LastTicks) / TickFrequency;
                bucket.LastTicks = now;

                bucket.Tokens = Math.Min(capacity, bucket.Tokens + elapsedSeconds * ratePerSecond);

                if (bucket.Tokens < 1.0) return false;
                bucket.Tokens -= 1.0;
                return true;
            }
        }
    }
}
