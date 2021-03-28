using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport
{
    public class CrossoverTransport : ITransport
    {
        private readonly IDictionary<MultiAddress, ConnectionListener> _listeners =
            new Dictionary<MultiAddress, ConnectionListener>();
        
        public async Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            if (!_listeners.TryGetValue(address, out var listener))
            {
                throw new Exception($"Unknown address {address}, cannot connect.");
            }

            var connectorToListenerPipe = new Pipe();
            var listenerToConnectorPipe = new Pipe();
            var connectorConnection = new PipeConnection(listenerToConnectorPipe.Reader, connectorToListenerPipe.Writer);
            var listenerConnection = new PipeConnection(connectorToListenerPipe.Reader, listenerToConnectorPipe.Writer);

            await listener.ConnectionChannel.Writer.WriteAsync(listenerConnection, cancellationToken).ConfigureAwait(false);

            return connectorConnection;
        }

        public Task<IConnectionListener> ListenAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            var listener = new ConnectionListener();
            _listeners[address] = listener;
            return Task.FromResult<IConnectionListener>(listener);
        }

        class ConnectionListener : IConnectionListener
        {
            public readonly Channel<IConnection> ConnectionChannel = Channel.CreateUnbounded<IConnection>();
            
            public void Dispose()
            {
            }

            public async Task<IConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
            {
                var connection = await ConnectionChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
        }
    }
}
