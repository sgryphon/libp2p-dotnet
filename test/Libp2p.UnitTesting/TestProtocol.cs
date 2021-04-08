using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.UnitTesting
{
    public class TestProtocol : IProtocol
    {
        public IList<IConnection> Connections { get; } = new List<IConnection>();

        public string Name => "TestProtocol";

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            Connections.Add(connection);
            return Task.CompletedTask;
        }
    }
}
