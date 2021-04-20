using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net
{
    public class Libp2pClient : IDisposable
    {
        private readonly List<ITransport> _transports = new List<ITransport>();
        private ITransportListener? _transportListener;
        private readonly IConnectionUpgrader _connectionUpgrader;

        public Libp2pClient(IList<ITransport> transports, IConnectionUpgrader connectionUpgrader)
        {
            _transports.AddRange(transports);
            _connectionUpgrader = connectionUpgrader;
        }

        public async Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            // TODO: Support multiple transports
            var transportConnection = await _transports[0].ConnectAsync(address, cancellationToken).ConfigureAwait(false);
            var connection = await _connectionUpgrader.UpgradeAsync(transportConnection, cancellationToken)
                .ConfigureAwait(false);
            return connection;
        }

        public async Task ListenAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            // TODO: Support multiple listeners on same transport, and multiple transports
            if (_transportListener != null)
            {
                throw new InvalidOperationException($"LibP2P client is already listening on {_transportListener.LocalAddress}");
            }
            
            _transportListener =  await _transports[0].ListenAsync(address, cancellationToken).ConfigureAwait(false);
        }
        
        public async Task<IConnection> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_transportListener == null)
            {
                throw new InvalidOperationException($"LibP2P client is not listening yet; you need need to start listening before accepting connections");
            }

            var transportConnection = await _transportListener.AcceptAsync(cancellationToken).ConfigureAwait(false);
            var connection = await _connectionUpgrader.UpgradeAsync(transportConnection, cancellationToken)
                .ConfigureAwait(false);
            return connection;
        }

        public void Dispose()
        {
            _transportListener?.Dispose();
        }
    }
}
