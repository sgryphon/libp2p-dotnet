using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocolSelect
    {
        Task<T?> SelectProtocolAsync<T>(IPipeline pipeline, T protocol, CancellationToken cancellationToken = default)
            where T : class, IProtocol;

        Task<T?> ListenProtocolAsync<T>(IPipeline pipeline, IList<T> protocols,
            CancellationToken cancellationToken = default)
            where T : class, IProtocol;
    }
}
