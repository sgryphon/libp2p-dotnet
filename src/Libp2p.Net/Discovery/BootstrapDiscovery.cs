using System.Collections.Generic;

namespace Libp2p.Net.Discovery
{
    public class BootstrapDiscovery : IDiscovery
    {
        private readonly List<string> _bootstrapAddresses = new List<string>();

        // should this be Libp2p.Peer.Discovery ?
        public BootstrapDiscovery(IList<string> bootstrapAddresses)
        {
            _bootstrapAddresses.AddRange(bootstrapAddresses);
        }
    }
}
