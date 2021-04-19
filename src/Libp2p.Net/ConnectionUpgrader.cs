using System.Threading;

namespace Libp2p.Net
{
    public class ConnectionUpgrader : IConnectionUpgrader
    {
        
        public IConnection UpgradeAsync(ITransportConnection transportConnection, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
