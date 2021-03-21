using System.Net.Sockets;
using System.Threading.Tasks;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpConnectionListener : IConnectionListener
    {
        private readonly TcpListener _tcpListener;

        internal TcpConnectionListener(TcpListener tcpListener)
        {
            _tcpListener = tcpListener;
        }
        
        public async Task<IConnection> AcceptConnectionAsync()
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            var connection = new TcpConnection(tcpClient);
            return connection;
        }
    }
}
