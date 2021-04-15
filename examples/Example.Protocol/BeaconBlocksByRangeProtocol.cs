using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Example.Protocol
{
    public class BeaconBlocksByRangeProtocol : IProtocol
    {
        public string Name => "BeaconBlocksByRange";

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
