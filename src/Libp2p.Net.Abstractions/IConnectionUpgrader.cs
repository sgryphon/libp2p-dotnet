using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IConnectionUpgrader
    {
        Task<IConnection> UpgradeAsync(ITransportConnection transportConnection, CancellationToken cancellationToken = default);
    }
}
