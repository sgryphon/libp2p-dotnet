using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.Peering
{
    public class PeerPool
    {
        public void ConfigureConnect(IList<ITransport> transports, IList<IProtocolSelect> selectors, IList<IEncryptionProtocol> encryptors, IList<IMultiplexProtocol> multiplexers)
        {
            //throw new NotImplementedException();
        }

        public void ConfigureListen(IList<ITransport> transports, IList<IProtocolSelect> selectors, IList<IEncryptionProtocol> encryptors, IList<IMultiplexProtocol> multiplexers)
        {
            //throw new NotImplementedException();
        }

        public void AddDiscovery(IList<IDiscovery> discovery)
        {
            //throw new NotImplementedException();
        }

        public int MinimumDesired { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);
            //throw new NotImplementedException();
        }

        public async Task<IConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(5000, cancellationToken);
            //throw new NotImplementedException();
            return null;
        }
    }
}
