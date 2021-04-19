using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.Peering
{
    public class PeerPool
    {
        private readonly List<IDiscovery> _discovery = new List<IDiscovery>();

        public void AddDiscovery(IList<IDiscovery> discovery)
        {
            _discovery.AddRange(discovery);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);
            //throw new NotImplementedException();
        }
    }
}
