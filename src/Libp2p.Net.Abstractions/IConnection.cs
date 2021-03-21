using System;
using System.IO;

namespace Libp2p.Net
{
    public interface IConnection : IDisposable
    {
        Stream GetStream();
    }
}
