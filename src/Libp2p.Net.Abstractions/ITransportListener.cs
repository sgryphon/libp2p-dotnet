using System;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface ITransportListener : IDisposable
    {
        MultiAddress? LocalAddress { get; }
        Task<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default);
    }
}
