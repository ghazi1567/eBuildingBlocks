using eBuildingBlocks.SMPP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
namespace eBuildingBlocks.SMPP.Tcp
{
  


    public sealed class SmppTcpListener
    {
        private readonly IPEndPoint _endpoint;
        private readonly ISmppAuthenticator _auth;
        private readonly ISmppMessageHandler _handler;
        private readonly IServiceProvider _sp;
        public SmppTcpListener(IPEndPoint endpoint, IServiceProvider serviceProvider,ISmppAuthenticator auth, ISmppMessageHandler handler)
        {
            _endpoint = endpoint;
            _auth = auth;
            _handler = handler;
            _sp = serviceProvider;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var bindRegistry = _sp.GetRequiredService<IBindRegistry>();
            var policy = _sp.GetRequiredService<ISmppSessionPolicy>();

            if (policy is null) throw new InvalidOperationException("Service Provider is note providing required.");
            if (bindRegistry is null) throw new InvalidOperationException("Service Provider is note providing BindRegistary.");


            var listener = new TcpListener(_endpoint);
            listener.Start();

            while (!ct.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() =>
                {
                    var conn = new SmppConnection(tcp, _auth, _handler, policy, bindRegistry);
                    return conn.RunAsync(ct);
                }, ct);
            }
        }
    }

}
