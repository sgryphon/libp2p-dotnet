using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IMultiplexProtocol
    {
        public Task<IMultiplexer> StartMultiplexerAsync(IConnection connection, CancellationToken cancellationToken = default);
        //public Task<IConnectionListener> ListenAsync(CancellationToken cancellationToken = default);
    }
}
