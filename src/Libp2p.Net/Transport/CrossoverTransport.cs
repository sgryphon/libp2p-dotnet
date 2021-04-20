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
        private readonly IDictionary<MultiAddress, TransportListener> _listeners =
            new Dictionary<MultiAddress, TransportListener>();

        private int _nextAddressId;

        public string Name => "Crossover";

        public async Task<ITransportConnection> ConnectAsync(MultiAddress remoteAddress, CancellationToken cancellationToken = default)
        {
            if (!_listeners.TryGetValue(remoteAddress, out var listener))
            {
                throw new Exception($"Unknown address {remoteAddress}, cannot connect.");
            }

            var localAddressId = Interlocked.Increment(ref _nextAddressId);
            var localAddress = MultiAddress.Parse($"/memory/{localAddressId}");

            var connectorToListenerPipe = new Pipe();
            var listenerToConnectorPipe = new Pipe();
            var connectorConnection =
                new PipeConnection(localAddress, remoteAddress, Direction.Outbound,
                    listenerToConnectorPipe.Reader, connectorToListenerPipe.Writer);

            // For the other side, the address is the remote address
            var listenerConnection = new PipeConnection(remoteAddress, localAddress, Direction.Inbound,
                connectorToListenerPipe.Reader, listenerToConnectorPipe.Writer);
            await listener.ConnectionChannel.Writer.WriteAsync(listenerConnection, cancellationToken)
                .ConfigureAwait(false);

            return connectorConnection;
        }

        public Task<ITransportListener> ListenAsync(MultiAddress localAddress,
            CancellationToken cancellationToken = default)
        {
            var listener = new TransportListener(localAddress);
            _listeners[localAddress] = listener;
            return Task.FromResult<ITransportListener>(listener);
        }

        private class TransportListener : ITransportListener
        {
            public readonly Channel<ITransportConnection> ConnectionChannel =
                Channel.CreateUnbounded<ITransportConnection>();

            public TransportListener(MultiAddress localAddress)
            {
                LocalAddress = localAddress;
            }

            public MultiAddress? LocalAddress { get; }

            public async Task<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default)
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
