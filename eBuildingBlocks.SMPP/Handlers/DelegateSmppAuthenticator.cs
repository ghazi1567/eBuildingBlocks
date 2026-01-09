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
        private readonly Func<SmppAuthContext, Task<bool>> _auth;

        public DelegateSmppAuthenticator(Func<SmppAuthContext, Task<bool>> auth)
        {
            _auth = auth;
        }

        public Task<bool> AuthenticateAsync(SmppAuthContext context) => _auth(context);
    }

}
