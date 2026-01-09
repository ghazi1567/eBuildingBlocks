using eBuildingBlocks.SMPP.Models;
using System.Collections.Concurrent;

namespace eBuildingBlocks.SMPP.Reassembly
{
    /// <summary>
    /// In-memory multipart SMS reassembler.
    /// Intended for testing, demos, and single-node deployments.
    /// </summary>
    public sealed class InMemorySmppMessageReassembler : ISmppMessageReassembler
    {
        private readonly ConcurrentDictionary<MultipartKey, MultipartState> _store = new();
        private readonly TimeSpan _ttl;

        /// <param name="ttl">
        /// Time to keep incomplete multipart messages before cleanup.
        /// Default is 5 minutes.
        /// </param>
        public InMemorySmppMessageReassembler(TimeSpan? ttl = null)
        {
            _ttl = ttl ?? TimeSpan.FromMinutes(5);
        }

        public ReassembledMessage? TryAddPart(
            SmppSessionContext session,
            SmppSubmitRequest request)
        {
            // SHORT MESSAGE → immediately complete
            if (request.Concat == null)
            {
                return new ReassembledMessage(
                    session.SystemId,
                    request.SourceAddr,
                    request.DestinationAddr,
                    request.DataCoding,
                    request.UserPayloadBytes);
            }

            CleanupExpired();

            var key = new MultipartKey(
                session.SystemId,
                request.SourceAddr,
                request.DestinationAddr,
                request.Concat.Ref);

            var state = _store.GetOrAdd(key, _ =>
                new MultipartState(request.Concat.Total, request.DataCoding));

            lock (state)
            {
                // Ignore duplicate segments
                state.Parts.TryAdd(
                    request.Concat.Seq,
                    request.UserPayloadBytes);

                // Not complete yet
                if (state.Parts.Count < state.TotalParts)
                    return null;

                // Reassemble in correct order
                var fullPayload = state.Parts
                    .OrderBy(p => p.Key)
                    .SelectMany(p => p.Value)
                    .ToArray();

                //  IMPORTANT: remove immediately after reassembly
                _store.TryRemove(key, out _);

                return new ReassembledMessage(
                    session.SystemId,
                    request.SourceAddr,
                    request.DestinationAddr,
                    request.DataCoding,
                    fullPayload);
            }
        }

        /// <summary>
        /// Removes expired incomplete multipart messages.
        /// </summary>
        private void CleanupExpired()
        {
            var expiry = DateTime.UtcNow - _ttl;

            foreach (var kv in _store)
            {
                if (kv.Value.CreatedAt < expiry)
                {
                    _store.TryRemove(kv.Key, out _);
                }
            }
        }

        // ============================
        // Internal helper types
        // ============================

        private readonly record struct MultipartKey(
            string SystemId,
            string SourceAddr,
            string DestinationAddr,
            int ReferenceNumber
        );

        private sealed class MultipartState
        {
            public int TotalParts { get; }
            public byte DataCoding { get; }
            public DateTime CreatedAt { get; } = DateTime.UtcNow;

            // seq → payload
            public ConcurrentDictionary<int, byte[]> Parts { get; } = new();

            public MultipartState(int totalParts, byte dataCoding)
            {
                TotalParts = totalParts;
                DataCoding = dataCoding;
            }
        }
    }

}
