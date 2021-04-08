using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocolMultiplex : IProtocol
    {
        public Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default);
        public Task<IConnectionListener> ListenAsync(CancellationToken cancellationToken = default);
    }
}
