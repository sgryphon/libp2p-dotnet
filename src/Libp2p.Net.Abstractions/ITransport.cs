using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface ITransport
    {
        string Name { get; }

        Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default);

        Task<IConnectionListener> ListenAsync(MultiAddress address, CancellationToken cancellationToken = default);
    }
}
