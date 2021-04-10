using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IMultiplexer
    {
        public Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
