
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Example.Protocol
{
    public class StatusProtocol : IProtocol
    {
        public string Identifier => "/eth2/beacon_chain/req/status/1/";

        public Task StartAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
