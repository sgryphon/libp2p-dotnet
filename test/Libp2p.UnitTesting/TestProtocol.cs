using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.UnitTesting
{
    public class TestProtocol : IProtocol
    {
        public TestProtocol(string identifier)
        {
            Identifier = identifier;
        }
        
        public string Identifier { get; }
    }
}
