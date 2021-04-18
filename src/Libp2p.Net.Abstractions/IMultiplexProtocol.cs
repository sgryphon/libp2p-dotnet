using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IMultiplexProtocol : IProtocol
    {
        Task<IMultiplexer> StartMultiplexerAsync(IPipeline pipeline, ITransportConnection transportConnection,
            CancellationToken cancellationToken = default);
        //public Task<IConnectionListener> ListenAsync(CancellationToken cancellationToken = default);
    }
}
