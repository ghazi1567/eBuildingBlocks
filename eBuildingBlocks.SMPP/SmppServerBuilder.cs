using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Handlers;
using eBuildingBlocks.SMPP.Models;
using eBuildingBlocks.SMPP.Session;
using eBuildingBlocks.SMPP.Tcp;
using System.Net;

namespace eBuildingBlocks.SMPP
{
    public sealed class SmppServerBuilder
    {
        private readonly List<IPEndPoint> _endpoints = new();

        private IPAddress _ipAddress = IPAddress.Any;
        private ISmppAuthenticator? _auth;
        private ISmppMessageHandler? _handler;
        public delegate Task<bool> SmppAuthenticateDelegate(ISmppAuthenticator context);
        private SmppServerBuilder() { }

        public static SmppServerBuilder Create() => new();


        /// <summary>
        /// Single endpoint listener
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public SmppServerBuilder ListenOn(IPAddress ipAddress, int port)
        {
            _endpoints.Add(new IPEndPoint(ipAddress, port));
            return this;
        }

        /// <summary>
        /// Listen on any IPs with specified port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public SmppServerBuilder ListenOn(int port)
        {
            _endpoints.Add(new IPEndPoint(IPAddress.Any, port));
            return this;
        }

        /// <summary>
        /// Listen on multiple endpoints 
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public SmppServerBuilder ListenOn(IEnumerable<IPEndPoint> endpoints)
        {
            _endpoints.AddRange(endpoints);
            return this;
        }

        public SmppServerBuilder WithAuthenticator(Func<SmppAuthContext, Task<SmppAuthResult>> authenticator)
        {
            _auth = new DelegateSmppAuthenticator(authenticator);
            return this;
        }

        public SmppServerBuilder WithAuthenticator(Func<SmppAuthContext, SmppAuthResult> authenticator)
        {
            _auth = new DelegateSmppAuthenticator(ctx => Task.FromResult(authenticator(ctx)));

            return this;
        }


        public SmppServerBuilder WithMessageHandler(Func<SmppSessionContext, SmppSubmitRequest, CancellationToken, Task<SmppSubmitResult>> handler)
        {
            _handler = new DelegateSmppMessageHandler(handler);
            return this;
        }


        public SmppServer Build(IServiceProvider serviceProvider)
        {
            if (_auth is null) throw new InvalidOperationException("Authenticator is required.");
            if (_handler is null) throw new InvalidOperationException("MessageHandler is required.");
            if (_endpoints.Count == 0) throw new InvalidOperationException("At least one endpoint is required.");

            var listeners = _endpoints
             .Distinct()
             .Select(ep => new SmppTcpListener(ep, serviceProvider, _auth, _handler))
             .ToList();

            return new SmppServer(listeners);
        }
    }

}
