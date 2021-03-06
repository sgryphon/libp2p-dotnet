using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpTransportListener : ITransportListener
    {
        private readonly TcpListener _tcpListener;

        internal TcpTransportListener(MultiAddress localAddress, TcpListener tcpListener)
        {
            LocalAddress = localAddress;
            _tcpListener = tcpListener;
        }

        public MultiAddress? LocalAddress { get; }

        public async Task<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default)
        {
            var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
            var remoteAddress = tcpClient.Client.RemoteEndPoint.ToMultiAddress();
            var connection = new TcpConnection(LocalAddress, remoteAddress, Direction.Inbound, tcpClient);
            return connection;
        }

        public void Dispose()
        {
            _tcpListener.Stop();
        }
    }
}
