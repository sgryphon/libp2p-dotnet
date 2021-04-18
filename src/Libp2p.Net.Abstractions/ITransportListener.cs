using System;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface ITransportListener : IDisposable
    {
        Task<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default);
    }
}
