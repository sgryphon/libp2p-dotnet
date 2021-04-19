using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface IConnection
    {
        Direction Direction { get; }
        
        MultiAddress LocalAddress { get; }
        
        MultiAddress RemoteAddress { get; }

        string RemotePeer { get; }

        string LocalPeer { get; }

        IMultiplexProtocol MultiplexProtocol { get; }
        
        IEncryptionProtocol EncryptionProtocol { get; }
        
        Task<(IPipeline, IProtocol)> ConnectAsync(IProtocol protocol, CancellationToken cancellationToken = default);
        
        Task<(IPipeline, IProtocol)> AcceptAsync(IList<IProtocol> protocol, CancellationToken cancellationToken = default);
    }
}
