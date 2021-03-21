using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Libp2p.Net;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    public class TcpTransport : ITransport
    {
        public async Task<IConnection> ConnectAsync(MultiAddress address)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port).ConfigureAwait(false);
            var connection = new TcpConnection(tcpClient);
            return connection;
        }

        public Task<IConnectionListener> ListenAsync(MultiAddress address)
        {
            var endpoint = address.ToIPEndPoint();
            var tcpListener = new TcpListener(endpoint);
            tcpListener.Start();
            var connectionListener = new TcpConnectionListener(tcpListener);
            return Task.FromResult<IConnectionListener>(connectionListener);
        }
    }
}
