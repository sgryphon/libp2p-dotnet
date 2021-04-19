using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.Peering
{
    public class ConnectionPool
    {
        private readonly List<IEncryptionProtocol> _encryptors = new List<IEncryptionProtocol>();
        private readonly List<IMultiplexProtocol> _multiplexers = new List<IMultiplexProtocol>();
        private readonly PeerPool _peerPool;
        private readonly List<IProtocolSelect> _selectors = new List<IProtocolSelect>();
        private readonly List<ITransport> _transports = new List<ITransport>();

        public ConnectionPool(PeerPool peerPool)
        {
            _peerPool = peerPool;
        }

        public int MinimumDesired { get; set; }

        public async Task<IConnection> AcceptAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(5000, cancellationToken);
            //throw new NotImplementedException();
            return null;
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

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(0, cancellationToken);
            //throw new NotImplementedException();
        }
    }
}
