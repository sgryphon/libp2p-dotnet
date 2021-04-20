using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface IConnection : IDisposable
    {
        Direction Direction { get; }

        IEncryptionProtocol? EncryptionProtocol { get; }

        MultiAddress? LocalAddress { get; }

        string LocalPeer { get; }

        IMultiplexProtocol? MultiplexProtocol { get; }

        MultiAddress RemoteAddress { get; }

        string RemotePeer { get; }

        Task<(IPipeline, IProtocol)> AcceptAsync(IList<IProtocol> protocols,
            CancellationToken cancellationToken = default);

        Task<(IPipeline?, IProtocol?)> ConnectAsync(IProtocol protocol, CancellationToken cancellationToken = default);
    }
}
