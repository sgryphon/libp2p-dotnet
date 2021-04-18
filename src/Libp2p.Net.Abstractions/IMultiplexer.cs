using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IMultiplexer
    {
        Task<IPipeline> ConnectAsync(CancellationToken cancellationToken = default);
        Task<IPipeline> AcceptAsync(CancellationToken cancellationToken = default);
    }
}
