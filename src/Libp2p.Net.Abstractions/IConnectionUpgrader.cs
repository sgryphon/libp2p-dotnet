using System.Threading;

namespace Libp2p.Net
{
    public interface IConnectionUpgrader
    {
        IConnection UpgradeAsync(ITransportConnection transportConnection, CancellationToken cancellationToken = default);
    }
}
