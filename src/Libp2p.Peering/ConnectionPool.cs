using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.Peering
{
    public class ConnectionPool
    {
        private readonly PeerPool _peerPool;
        private List<ITransport> _transports = new List<ITransport>();
        private List<IProtocolSelect> _selectors = new List<IProtocolSelect>();
        private List<IEncryptionProtocol> _encryptors = new List<IEncryptionProtocol>();
        private List<IMultiplexProtocol> _multiplexers = new List<IMultiplexProtocol>();

        public ConnectionPool(PeerPool peerPool)
        {
            _peerPool = peerPool;
        }
        
        public void Configure(IList<ITransport> transports, 
            IList<IProtocolSelect> selectors,
            IList<IEncryptionProtocol> encryptors, 
            IList<IMultiplexProtocol> multiplexers)
        {
            _transports.AddRange(transports);
            _selectors.AddRange(selectors);
            _encryptors.AddRange(encryptors);
            _multiplexers.AddRange(multiplexers);
        }

        public async Task<IConnection> AcceptAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(5000, cancellationToken);
            //throw new NotImplementedException();
            return null;
        }

        public int MinimumDesired { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);
            //throw new NotImplementedException();
        }

    }
}
