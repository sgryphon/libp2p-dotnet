using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpTransportListener : ITransportListener
    {
        private readonly TcpListener _tcpListener;

        internal TcpTransportListener(TcpListener tcpListener)
        {
            _tcpListener = tcpListener;
        }

        public async Task<ITransportConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            var address = tcpClient.Client.RemoteEndPoint.ToMultiAddress();
            var connection = new TcpConnection(Direction.Inbound, address, tcpClient);
            return connection;
        }

        public void Dispose()
        {
            _tcpListener.Stop();
        }
    }
}
