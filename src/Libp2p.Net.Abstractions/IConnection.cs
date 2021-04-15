using System;
using System.IO;
using System.IO.Pipelines;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface IConnection : IDuplexPipe, IDisposable
    {
        Direction Direction { get; }
        MultiAddress RemoteAddress { get; }
    }
}
