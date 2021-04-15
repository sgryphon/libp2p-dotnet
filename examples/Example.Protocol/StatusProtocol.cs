
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Example.Protocol
{
    public class StatusProtocol : IProtocol
    {
        public string Name => "StatusProtocol";

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
