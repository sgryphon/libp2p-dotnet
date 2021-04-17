using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public class Libp2pClient
    {
        private readonly ITransport _transport;

        public Libp2pClient(ITransport transport)
        {
            _transport = transport;
        }

        public async Task<ITransportConnection> ConnectAsync(MultiAddress address)
        {
            return await _transport.ConnectAsync(address).ConfigureAwait(false);
        }

        public async Task<ITransportListener> ListenAsync(MultiAddress address)
        {
            return await _transport.ListenAsync(address).ConfigureAwait(false);
        }
    }
}
