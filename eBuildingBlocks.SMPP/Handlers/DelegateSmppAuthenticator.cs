using eBuildingBlocks.SMPP.Abstractions;
using eBuildingBlocks.SMPP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.SMPP.Handlers
{
    internal sealed class DelegateSmppAuthenticator : ISmppAuthenticator
    {
        private readonly Func<SmppAuthContext, Task<SmppAuthResult>> _auth;

        public DelegateSmppAuthenticator(Func<SmppAuthContext, Task<SmppAuthResult>> auth)
        {
            _auth = auth;
        }

        public async Task<SmppAuthResult> AuthenticateAsync(SmppAuthContext context)
        {
            var result = await _auth(context);

            if (result.Success && result.Policy != null)
            {
                context.Session.Policy = result.Policy; 
            }

            return result;
        }
    }

}
