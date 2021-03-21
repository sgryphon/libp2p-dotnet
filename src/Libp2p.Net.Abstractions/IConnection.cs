using System.IO;

namespace Libp2p.Net
{
    public interface IConnection
    {
        Stream GetStream();
    }
}
