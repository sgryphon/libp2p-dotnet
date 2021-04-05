using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocol
    {
        string Name { get; }
        Task StartAsync(IConnection connection, CancellationToken cancellationToken = default);
    }
}
