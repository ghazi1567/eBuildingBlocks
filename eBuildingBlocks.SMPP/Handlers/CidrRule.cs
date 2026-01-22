using eBuildingBlocks.SMPP.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace eBuildingBlocks.SMPP.Handlers
{
    public sealed class CidrRule : IIpRule
    {
        private readonly byte[] _network;
        private readonly int _prefix;
        private readonly AddressFamily _family;

        public CidrRule(IPAddress network, int prefix)
        {
            _family = network.AddressFamily;
            _network = network.GetAddressBytes();
            _prefix = prefix;
        }

        public bool Matches(IPAddress ip)
        {
            if (ip.AddressFamily != _family) return false;

            var bytes = ip.GetAddressBytes();
            int fullBytes = _prefix / 8;
            int remBits = _prefix % 8;

            for (int i = 0; i < fullBytes; i++)
                if (bytes[i] != _network[i]) return false;

            if (remBits == 0) return true;

            byte mask = (byte)(0xFF << (8 - remBits));
            return (bytes[fullBytes] & mask) == (_network[fullBytes] & mask);
        }
    }

}
