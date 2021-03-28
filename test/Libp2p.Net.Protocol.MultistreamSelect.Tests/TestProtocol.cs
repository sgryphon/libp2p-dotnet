using System.Collections.Generic;

namespace Libp2p.Net.Protocol.Tests
{
    public class TestProtocol : IProtocol
    {
        public IList<IConnection> Connections = new List<IConnection>();
        public void Start(IConnection connection)
        {
            Connections.Add(connection);
        }
    }
}
