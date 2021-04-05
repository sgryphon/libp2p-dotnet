using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocol
    {
        Task StartAsync(IConnection connection, CancellationToken cancellationToken = default);
        string Name { get; }
    }
}
