using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eBuildingBlocks.SMPP.Abstractions;
using System.Net;
using System.Net.Sockets;
namespace eBuildingBlocks.SMPP.Tcp
{
  


    public sealed class SmppTcpListener
    {
        private readonly IPEndPoint _endpoint;
        private readonly ISmppAuthenticator _auth;
        private readonly ISmppMessageHandler _handler;
        private readonly ISmppSessionPolicy _policy;

        public SmppTcpListener(IPEndPoint endpoint, ISmppAuthenticator auth, ISmppMessageHandler handler, ISmppSessionPolicy policy)
        {
            _endpoint = endpoint;
            _auth = auth;
            _handler = handler;
            _policy = policy;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var listener = new TcpListener(_endpoint);
            listener.Start();

            while (!ct.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() =>
                {
                    var conn = new SmppConnection(tcp, _auth, _handler, _policy);
                    return conn.RunAsync(ct);
                }, ct);
            }
        }
    }

}
