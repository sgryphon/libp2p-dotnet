using System;
using System.IO;
using System.IO.Pipelines;

namespace Libp2p.Net
{
    public interface IConnection : IDisposable, IDuplexPipe
    {
    }
}
