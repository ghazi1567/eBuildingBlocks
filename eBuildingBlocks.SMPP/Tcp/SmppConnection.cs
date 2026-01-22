using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Parsing;
using eBuildingBlocks.SMPP.Protocol;
using eBuildingBlocks.SMPP.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private readonly SmppSessionContext _session = new();

        private readonly ISmppAuthenticator _auth;
        private readonly ISmppMessageHandler _handler;
        private readonly ISmppSessionPolicy _policy;
        private readonly IBindRegistry _bindRegistry;
        public SmppConnection(TcpClient tcp, ISmppAuthenticator auth, ISmppMessageHandler handler, ISmppSessionPolicy policy, IBindRegistry bindRegistry)
        {
            _tcp = tcp;
            _stream = tcp.GetStream();

            var remote = (IPEndPoint)tcp.Client.RemoteEndPoint!;
            var local = (IPEndPoint)tcp.Client.LocalEndPoint!;
            _session.RemoteIp = remote.Address;
            _session.LocalPort = local.Port;


            _auth = auth;
            _handler = handler;
            _policy = policy;
            _bindRegistry = bindRegistry;
        }

        public async Task RunAsync(CancellationToken ct)
        {

            Logger.Debug(this.GetType().Name, "Run");
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
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _bindRegistry.Unregister(_session);
                _session.State = SmppSessionState.Closed;
                _tcp.Close();
            }
        }

        private async Task DispatchAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            Logger.Debug(this.GetType().Name, "Dispatch");
            switch (h.CommandId)
            {
                case SmppCommandIds.bind_receiver:
                case SmppCommandIds.bind_transmitter:
                case SmppCommandIds.bind_transceiver:
                    await HandleBindAsync(h, pdu, ct);
                    return;

                case SmppCommandIds.enquire_link:
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.enquire_link_resp, 0, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    return;

                case SmppCommandIds.unbind:
                    _bindRegistry.Unregister(_session);
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.unbind_resp, 0, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    _session.State = SmppSessionState.Closed;
                    return;

                case SmppCommandIds.submit_sm:
                    await HandleSubmitSmAsync(h, pdu, ct);
                    return;

                default:
                    // Unknown: respond with invalid cmd id
                    await WriteAsync(SmppPduWriter.BuildResponse(h.CommandId | 0x80000000, (uint)SmppCommandStatus.ESME_RINVCMDID, h.Sequence, ReadOnlySpan<byte>.Empty), ct);
                    return;
            }
        }

        private async Task HandleBindAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            Logger.Debug(this.GetType().Name, "Bind");
            var respId = ToBindRespId(h.CommandId);
            // Body begins at 16
            var span = (ReadOnlySpan<byte>)pdu;
            int o = 16;

            string systemId = SmppPduReader.ReadCString(span, ref o);
            string password = SmppPduReader.ReadCString(span, ref o);

            string systemType = SmppPduReader.ReadCString(span, ref o);

            byte interfaceVersion = SmppPduReader.ReadByte(span, ref o);
            byte addrTon = SmppPduReader.ReadByte(span, ref o);
            byte addrNpi = SmppPduReader.ReadByte(span, ref o);
            string addressRange = SmppPduReader.ReadCString(span, ref o);

            if (_session.State != SmppSessionState.Open)
            {
                await WriteAsync(
                    SmppPduWriter.BuildResponse(
                        respId,
                        (uint)SmppCommandStatus.ESME_RALYBND,
                        h.Sequence,
                        SmppPduWriter.CString("")),
                    ct);
                return;
            }

            _session.AddrTon = addrTon;
            _session.AddrNpi = addrNpi;
            _session.AddressRange = addressRange;


            var authContext = new SmppAuthContext(
                  systemId,
                  password,
                  ToBindMode(h.CommandId),
                  string.IsNullOrWhiteSpace(systemType) ? null : systemType,
                  interfaceVersion,
                  _session.RemoteIp,
                  _session.LocalPort,
                  _session
              );

            SmppAuthResult result;
            try
            {
                result = await _auth.AuthenticateAsync(authContext);
                Logger.Debug(this.GetType().Name, $"Auth Result : {result.Success}");
            }
            catch
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(SmppPduWriter.BuildResponse(respId, (uint)SmppCommandStatus.ESME_RSYSERR, h.Sequence, body), ct);
                return;
            }

            if (!result.Success)
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(
                    SmppPduWriter.BuildResponse(
                        respId, // is not it should be according to GetBindMode
                        result.CommandStatus,
                        h.Sequence,
                        body),
                    ct);

                return;
            }
            _session.Policy = result.Policy;

            var policyResult = await _policy.ValidateBind(authContext, _session);

            if (!policyResult.Allowed)
            {
                Logger.Debug(this.GetType().Name, $"policyResult : {policyResult.Allowed}");
                await WriteAsync(
                    SmppPduWriter.BuildResponse(
                        respId,
                        policyResult.CommandStatus,
                        h.Sequence,
                        SmppPduWriter.CString("")),
                    ct);
                return;
            }

            _session.SystemId = systemId;
            _session.BindMode = authContext.RequestedBindMode;
         
            _session.State = authContext.RequestedBindMode switch
            {
                SmppBindMode.Transmitter => SmppSessionState.BoundTx,
                SmppBindMode.Receiver => SmppSessionState.BoundRx,
                _ => SmppSessionState.BoundTrx
            };
            _bindRegistry.Register(_session);

            Logger.Debug(this.GetType().Name, $"Completed Dispatch");
            // bind_resp has system_id (server name)
            var respBody = SmppPduWriter.CString("SmppLiteServer");
            await WriteAsync(SmppPduWriter.BuildResponse(respId, 0, h.Sequence, respBody), ct);
        }

        private async Task HandleSubmitSmAsync(SmppHeader h, byte[] pdu, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_session.SystemId) || (_session.State != SmppSessionState.BoundTrx && _session.State != SmppSessionState.BoundTx))
            {
                var body = SmppPduWriter.CString("");
                await WriteAsync(
                    SmppPduWriter.BuildResponse(
                        SmppCommandIds.submit_sm_resp,
                        (uint)SmppCommandStatus.ESME_RINVBNDSTS,
                        h.Sequence,
                        body),
                    ct);
                return;
            }


            // In-flight enforcement
            int inFlight = Interlocked.Increment(ref _session.InFlightSubmits);
            try
            {
                var maxInFlight = await _policy.GetMaxInFlight(_session);

                if (inFlight > maxInFlight)
                {
                    var body = SmppPduWriter.CString("");
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.ESME_RTHROTTLED, h.Sequence, body), ct);
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
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.ESME_RSYSERR, h.Sequence, body), ct);
                    return;
                }
                var policyResult = await _policy.ValidateSubmit(_session, req);
                if (!policyResult.Allowed)
                {
                    await WriteAsync(
                        SmppPduWriter.BuildResponse(
                            SmppCommandIds.submit_sm_resp,
                            policyResult.CommandStatus,
                            h.Sequence,
                            SmppPduWriter.CString("")),
                        ct);
                    return;
                }
                SmppSubmitResult result;
                try
                {
                    result = await _handler.HandleSubmitAsync(_session, req, ct);
                }
                catch
                {
                    // Handler failure should not crash the session; return SYS_ERROR.
                    var body = SmppPduWriter.CString("");
                    await WriteAsync(SmppPduWriter.BuildResponse(SmppCommandIds.submit_sm_resp, (uint)SmppCommandStatus.ESME_RSYSERR, h.Sequence, body), ct);
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

        private static SmppBindMode ToBindMode(uint commandId) => commandId switch
        {
            SmppCommandIds.bind_transmitter => SmppBindMode.Transmitter,
            SmppCommandIds.bind_receiver => SmppBindMode.Receiver,
            SmppCommandIds.bind_transceiver => SmppBindMode.Transceiver,
            _ => throw new InvalidOperationException("Not a bind command")
        };

        private static uint ToBindRespId(uint commandId) => commandId switch
        {
            SmppCommandIds.bind_transmitter => SmppCommandIds.bind_transmitter_resp,
            SmppCommandIds.bind_receiver => SmppCommandIds.bind_receiver_resp,
            SmppCommandIds.bind_transceiver => SmppCommandIds.bind_transceiver_resp,
            _ => commandId | 0x80000000
        };

    }

}
