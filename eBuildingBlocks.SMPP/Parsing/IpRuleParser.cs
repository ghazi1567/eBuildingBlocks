using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Handlers;
using System.Net;

namespace eBuildingBlocks.SMPP.Parsing
{
    public static class IpRuleParser
    {
        public static List<IIpRule> ParseRules(string[] rawRules)
        {
            var list = new List<IIpRule>();
            foreach (var raw in rawRules ?? Array.Empty<string>())
            {
                var s = raw.Trim();
                if (string.IsNullOrEmpty(s)) continue;

                // CIDR
                var slash = s.IndexOf('/');
                if (slash > 0)
                {
                    var ipPart = s[..slash];
                    var prefixPart = s[(slash + 1)..];

                    if (IPAddress.TryParse(ipPart, out var net) && int.TryParse(prefixPart, out var prefix))
                    {
                        list.Add(new CidrRule(net, prefix));
                    }
                    continue;
                }

                // exact
                if (IPAddress.TryParse(s, out var ip))
                    list.Add(new ExactIpRule(ip));
            }
            return list;
        }
    }
}
