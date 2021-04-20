using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IMultiplexProtocol : IProtocol
    {
        Task<IMultiplexer> StartMultiplexerAsync(IPipeline pipeline, CancellationToken cancellationToken = default);
    }
}
