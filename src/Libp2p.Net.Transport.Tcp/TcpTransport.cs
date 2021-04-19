using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    public class TcpTransport : ITransport
    {
        public string Name => "TCP";

        public async Task<ITransportConnection> ConnectAsync(MultiAddress address,
            CancellationToken cancellationToken = default)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            var connection = new TcpConnection(Direction.Outbound, address, tcpClient);
            return connection;
        }

        public Task<ITransportListener> ListenAsync(MultiAddress address,
            CancellationToken cancellationToken = default)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpListener = new TcpListener(endpoint);
            tcpListener.Start();
            var connectionListener = new TcpTransportListener(tcpListener);
            return Task.FromResult<ITransportListener>(connectionListener);
        }
    }
}
