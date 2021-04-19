using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Example.Protocol
{
    public class BeaconBlocksByRangeProtocol : IProtocol
    {
        public string Identifier => "/eth2/beacon_chain/req/beacon_blocks_by_range/1/";
                
        public Task HandleAsync(IConnection connection, IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        
        public Task StartAsync(IConnection connection, IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
