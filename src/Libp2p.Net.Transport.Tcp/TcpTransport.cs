using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    public class TcpTransport : ITransport
    {
        public string Name => "TCP";

        public async Task<ITransportConnection> ConnectAsync(MultiAddress remoteAddress,
            CancellationToken cancellationToken = default)
        {
            var endpoint = remoteAddress.ToIPEndPoint();
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            var localAddress = tcpClient.Client.LocalEndPoint.ToMultiAddress();
            var connection = new TcpConnection(localAddress, remoteAddress, Direction.Outbound, tcpClient);
            return connection;
        }

        public Task<ITransportListener> ListenAsync(MultiAddress localAddress,
            CancellationToken cancellationToken = default)
        {
            var endpoint = localAddress.ToIPEndPoint();
            var tcpListener = new TcpListener(endpoint);
            tcpListener.Start();
            var connectionListener = new TcpTransportListener(localAddress, tcpListener);
            return Task.FromResult<ITransportListener>(connectionListener);
        }
    }
}
