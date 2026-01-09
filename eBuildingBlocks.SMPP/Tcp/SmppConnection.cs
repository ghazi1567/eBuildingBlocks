using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Parsing;
using eBuildingBlocks.SMPP.Protocol;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Tcp
{
    public sealed class SmppConnection
    {
        private readonly TcpClient _tcp;
        private readonly NetworkStream _stream;
        private readonly SmppFrameDecoder _decoder = new();
        private readonly SmppSession _session = new();

        private readonly ISmppAuthenticator _auth;
        private readonly ISmppMessageHandler _handler;
        private readonly ISmppSessionPolicy _policy;

        public SmppConnection(TcpClient tcp, ISmppAuthenticator auth, ISmppMessageHandler handler, ISmppSessionPolicy policy)
        {
            _tcp = tcp;
            _stream = tcp.GetStream();
            _auth = auth;
            _handler = handler;
            _policy = policy;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var buf = new byte[64 * 1024];

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int read = await _stream.ReadAsync(buf, ct);
                    if (read <= 0) break;

                    _decoder.Append(buf.AsSpan(0, read));

                    while (_decoder.TryReadFrame(out var pdu))
                    {
                        var h = SmppPduReader.ReadHeader(pdu);
                        await DispatchAsync(h, pdu, ct);
                        if (_session.State == SmppSessionState.Closed) return;
                    }
                }
            }
            catch
            {
                // swallow; consumer can add logging around server level if desired
            }
            finally
            {
                _session.State = SmppSessionState.Closed;
                _tcp.Close();
            }
        }

        private async Task DispatchAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            switch (h.CommandId)
            {
                case SmppCommandIds.bind_transceiver:
                    await HandleBindTrxAsync(h, pdu, ct);
                    return;

                case SmppCommandIds.enquire_link:
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.enquire_link_resp, 0, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    return;

                case SmppCommandIds.unbind:
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.unbind_resp, 0, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    _session.State = SmppSessionState.Closed;
                    return;

                case SmppCommandIds.submit_sm:
                    await HandleSubmitSmAsync(h, pdu, ct);
                    return;

                default:
                    // Unknown: respond with invalid cmd id
                    await WriteAsync(SmppPduWriter.BuildResponse(h.CommandId | 0x80000000, (uint)SmppCommandStatus.INVALID_CMD_ID, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    return;
            }
        }

        private async Task HandleBindTrxAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            // Body begins at 16
            var span = (ReadOnlySpan<byte>)pdu;
            int o = 16;

            string systemId = SmppPduReader.ReadCString(span, ref o);
            string password = SmppPduReader.ReadCString(span, ref o);

            // system_type (ignored)
            _ = SmppPduReader.ReadCString(span, ref o);

            // Optional: interface_version, addr_ton, addr_npi, address_range exist after this,
            // but for "Lite" we accept and ignore remaining fields.

            if (!_policy.CanBind(systemId))
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.bind_transceiver_resp, (uint)SmppCommandStatus.BIND_FAIL, h.Sequence, body), ct);
                return;
            }

            bool ok = await _auth.AuthenticateAsync(systemId, password, ct);
            if (!ok)
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.bind_transceiver_resp, (uint)SmppCommandStatus.BIND_FAIL, h.Sequence, body), ct);
                return;
            }

            _session.SystemId = systemId;
            _session.State = SmppSessionState.BoundTrx;

            // bind_resp has system_id (server name)
            var respBody = SmppPduWriter.CString("SmppLiteServer");
            await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.bind_transceiver_resp, 0, h.Sequence, respBody), ct);
        }

        private async Task HandleSubmitSmAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            if (_session.State != SmppSessionState.BoundTrx || string.IsNullOrEmpty(_session.SystemId))
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.INVALID_BIND_STATE, h.Sequence, body), ct);
                return;
            }

            var sessionCtx = new SmppSessionContext(_session.SystemId!, _session.SessionId);

            if (!_policy.CanSubmit(sessionCtx))
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.THROTTLED, h.Sequence, body), ct);
                return;
            }

            // In-flight enforcement
            int inFlight = Interlocked.Increment(ref _session.InFlightSubmits);
            try
            {
                if (inFlight > _policy.MaxInFlightPerSession)
                {
                    var body = SmppPduWriter.CString("");
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.THROTTLED, h.Sequence, body), ct);
                    return;
                }

                SmppSubmitRequest req;
                try
                {
                    req = SubmitSmParser.Parse(pdu);
                }
                catch
                {
                    var body = SmppPduWriter.CString("");
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.SYS_ERROR, h.Sequence, body), ct);
                    return;
                }

                SmppSubmitResult result;
                try
                {
                    result = await _handler.HandleSubmitAsync(sessionCtx, req, ct);
                }
                catch
                {
                    // Handler failure should not crash the session; return SYS_ERROR.
                    var body = SmppPduWriter.CString("");
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.SYS_ERROR, h.Sequence, body), ct);
                    return;
                }

                // submit_sm_resp body = message_id (CString)
                var respBody = SmppPduWriter.CString(result.MessageId ?? "");
                await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)result.Status, h.Sequence, respBody), ct);
            }
            finally
            {
                Interlocked.Decrement(ref _session.InFlightSubmits);
            }
        }

        private async Task WriteAsync(byte[] data, CancellationToken ct)
        {
            await _stream.WriteAsync(data, ct);
        }

    }

}
