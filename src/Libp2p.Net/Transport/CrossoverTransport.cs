using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Channels;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Transport
{
    public class CrossoverTransport : ITransport
    {
        private IDictionary<MultiAddress, ConnectionListener> _listeners =
            new Dictionary<MultiAddress, ConnectionListener>();
        
        public async Task<IConnection> ConnectAsync(MultiAddress address)
        {
            if (!_listeners.TryGetValue(address, out var listener))
            {
                throw new Exception($"Unknown address {address}, cannot connect.");
            }

            var connectorToListenerPipe = new Pipe();
            var listenerToConnectorPipe = new Pipe();
            var connectorConnection = new PipeConnection(listenerToConnectorPipe.Reader, connectorToListenerPipe.Writer);
            var listenerConnection = new PipeConnection(connectorToListenerPipe.Reader, listenerToConnectorPipe.Writer);

            await listener.ConnectionChannel.Writer.WriteAsync(listenerConnection);

            return connectorConnection;
        }

        public Task<IConnectionListener> ListenAsync(MultiAddress address)
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

            public async Task<IConnection> AcceptConnectionAsync()
            {
                var connection = await ConnectionChannel.Reader.ReadAsync();
                return connection;
            }
        }
    }
}
