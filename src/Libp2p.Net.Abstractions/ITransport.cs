using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface ITransport
    {
        public Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default);

        public Task<IConnectionListener> ListenAsync(MultiAddress address,
            CancellationToken cancellationToken = default);
    }
}
