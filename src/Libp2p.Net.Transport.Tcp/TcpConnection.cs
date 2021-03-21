using System.IO;
using System.Net.Sockets;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpConnection : IConnection
    {
        private readonly TcpClient _tcpClient;

        internal TcpConnection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
        }

        public Stream GetStream()
        {
            return _tcpClient.GetStream();
        }
    }
}
