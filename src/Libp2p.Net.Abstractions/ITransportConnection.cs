using System;
using Multiformats.Net;

namespace Libp2p.Net
{
    public interface ITransportConnection : IPipeline, IDisposable
    {
        Direction Direction { get; }
        MultiAddress RemoteAddress { get; }
    }
}
