using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Parsing;
using eBuildingBlocks.SMPP.Session;
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
                var text = SmppTextDecoder.TryDecode(request.UserPayloadBytes, request.DataCoding);
                return new ReassembledMessage(
                    new ReassembledMessageMetadata
                    {
                        SystemId = session.SystemId,
                        SessionId = session.SessionId,
                        ReceivedAtUtc = DateTime.UtcNow,
                        ReferenceNumber = request.Concat?.Ref,
                        TotalParts = request.Concat?.Total
                    },
                    request.SourceAddress,
                    request.DestinationAddress,
                    request.DataCoding,
                    request.UserPayloadBytes,
                    text);
            }

            CleanupExpired();

            var key = new MultipartKey(
                session.SystemId,
                request.SourceAddress,
                request.DestinationAddress,
                request.Concat.Ref);

            var state = _store.GetOrAdd(key, _ =>
                new MultipartState(request.Concat.Total, request.DataCoding));

            lock (state)
            {
                if (state.DataCoding != request.DataCoding)
                {
                    // Mixed data_coding in multipart → invalid message
                    _store.TryRemove(key, out _);
                    return null;
                }

                var seq = request.Concat.Seq;
                var total = request.Concat.Total;

                if (seq <= 0 || seq > total)
                {
                    return null; // silently ignore invalid segment
                }

                // Ignore duplicate segments
                state.Parts.TryAdd(
                    request.Concat.Seq,
                    request.UserPayloadBytes);

                // Not complete yet
                if (state.Parts.Count < state.TotalParts)
                    return null;

                // Reassemble in correct order
                var orderedParts = state.Parts
                    .OrderBy(p => p.Key)
                    .Select(p => p.Value)
                    .ToList();

                var totalLength = orderedParts.Sum(p => p.Length);
                var buffer = new byte[totalLength];

                int offset = 0;
                foreach (var part in orderedParts)
                {
                    part.Span.CopyTo(buffer.AsSpan(offset));
                    offset += part.Length;
                }
                var fullPayload = buffer;
                var text = SmppTextDecoder.TryDecode(fullPayload, request.DataCoding);

                //  IMPORTANT: remove immediately after reassembly
                _store.TryRemove(key, out _);

                return new ReassembledMessage(
                    new ReassembledMessageMetadata
                    {
                        SystemId = session.SystemId,
                        SessionId = session.SessionId,
                        ReceivedAtUtc = DateTime.UtcNow,
                        ReferenceNumber = request.Concat.Ref,
                        TotalParts = request.Concat.Total
                    },
                    request.SourceAddress,
                    request.DestinationAddress,
                    request.DataCoding,
                    fullPayload,
                    text);
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
            public ConcurrentDictionary<int, ReadOnlyMemory<byte>> Parts { get; } = new();

            public MultipartState(int totalParts, byte dataCoding)
            {
                TotalParts = totalParts;
                DataCoding = dataCoding;
            }
        }
    }

}
