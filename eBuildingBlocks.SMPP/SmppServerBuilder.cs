using eBuildingBlocks.SMPP.Abstractions;
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
        private ISmppSessionPolicy? _policy;

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

        public SmppServerBuilder WithAuthenticator(ISmppAuthenticator authenticator)
        {
            _auth = authenticator;
            return this;
        }

        public SmppServerBuilder WithMessageHandler(ISmppMessageHandler handler)
        {
            _handler = handler;
            return this;
        }

        public SmppServerBuilder WithSessionPolicy(ISmppSessionPolicy policy)
        {
            _policy = policy;
            return this;
        }

        public SmppServer Build()
        {
            if (_auth is null) throw new InvalidOperationException("Authenticator is required.");
            if (_handler is null) throw new InvalidOperationException("MessageHandler is required.");
            if (_policy is null) throw new InvalidOperationException("SessionPolicy is required.");
            if (_endpoints.Count == 0) throw new InvalidOperationException("At least one endpoint is required.");

            var listeners = _endpoints
             .Distinct()
             .Select(ep => new SmppTcpListener(ep, _auth, _handler, _policy))
             .ToList();

            return new SmppServer(listeners);
        }
    }

}
