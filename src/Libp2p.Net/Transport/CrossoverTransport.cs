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

        private int _nextConnectionId;

        public string Name => "Crossover";

        public async Task<IConnection> ConnectAsync(MultiAddress address, CancellationToken cancellationToken = default)
        {
            if (!_listeners.TryGetValue(address, out var listener))
            {
                throw new Exception($"Unknown address {address}, cannot connect.");
            }

            var connectorToListenerPipe = new Pipe();
            var listenerToConnectorPipe = new Pipe();
            var connectorConnection =
                new PipeConnection(Direction.Outbound, address, listenerToConnectorPipe.Reader,
                    connectorToListenerPipe.Writer);

            var connectionId = Interlocked.Increment(ref _nextConnectionId);
            var localAddress = MultiAddress.Parse($"/memory/{connectionId}");
            var listenerConnection = new PipeConnection(Direction.Inbound, localAddress, connectorToListenerPipe.Reader,
                listenerToConnectorPipe.Writer);

            await listener.ConnectionChannel.Writer.WriteAsync(listenerConnection, cancellationToken)
                .ConfigureAwait(false);

            return connectorConnection;
        }

        public Task<IConnectionListener> ListenAsync(MultiAddress address,
            CancellationToken cancellationToken = default)
        {
            var listener = new ConnectionListener();
            _listeners[address] = listener;
            return Task.FromResult<IConnectionListener>(listener);
        }

        private class ConnectionListener : IConnectionListener
        {
            public readonly Channel<IConnection> ConnectionChannel = Channel.CreateUnbounded<IConnection>();

            public async Task<IConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
            {
                var connection = await ConnectionChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }

            public void Dispose()
            {
            }
        }
    }
}
