using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    public class TcpTransport : ITransport
    {
        public string Name => "TCP";

        public async Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            var connection = new TcpConnection(address, tcpClient);
            return connection;
        }

        public Task<IConnectionListener> ListenAsync(MultiAddress address,
            CancellationToken cancellationToken = default)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpListener = new TcpListener(endpoint);
            tcpListener.Start();
            var connectionListener = new TcpConnectionListener(tcpListener);
            return Task.FromResult<IConnectionListener>(connectionListener);
        }
    }
}
