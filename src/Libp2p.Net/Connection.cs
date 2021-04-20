using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;
using Multiformats.Net;

namespace Libp2p.Net
{
    public class Connection : IConnection
    {
        private readonly ITransportConnection _transportConnection;
        private readonly IMultiplexer _multiplexer;
        private readonly IList<IProtocolSelect> _selectors;

        internal Connection(ITransportConnection transportConnection, IEncryptionProtocol? encryptionProtocol, IMultiplexProtocol? multiplexProtocol, IMultiplexer multiplexer, IList<IProtocolSelect> selectors)
        {
            _transportConnection = transportConnection;
            EncryptionProtocol = encryptionProtocol;
            MultiplexProtocol = multiplexProtocol;
            _multiplexer = multiplexer;
            _selectors = selectors;
        }

        public Direction Direction => _transportConnection.Direction;
        public MultiAddress? LocalAddress => _transportConnection.LocalAddress;
        public MultiAddress RemoteAddress => _transportConnection.RemoteAddress;
        public string RemotePeer => string.Empty;
        public string LocalPeer => string.Empty;
        public IMultiplexProtocol? MultiplexProtocol { get; }
        public IEncryptionProtocol? EncryptionProtocol { get; }

        public async Task<(IPipeline?, IProtocol?)> ConnectAsync(IProtocol protocol, CancellationToken cancellationToken = default)
        {
            var pipeline = await _multiplexer.ConnectAsync(cancellationToken);
            // TODO: Support multiple selectors
            // TODO: Support multiple protocols
            var selector = _selectors[0];
            // TODO: Maybe need a TrySelectProtocol? Or is null good enough?
            var selectedProtocol = await selector.SelectProtocolAsync(pipeline, protocol, cancellationToken)
                .ConfigureAwait(false);
            if (selectedProtocol == null)
            {
                //throw new NotSupportedException($"Protocol {protocol.Identifier} not supported");
                //pipeline.Close();
                // TODO: Yuck, returning null, null ... Maybe TryConnectAsync()? Can you have an async with out params?
                return (null, null);
            }

            return (pipeline, selectedProtocol);
        }

        public async Task<(IPipeline, IProtocol)> AcceptAsync(IList<IProtocol> protocols, CancellationToken cancellationToken = default)
        {
            var pipeline = await _multiplexer.AcceptAsync(cancellationToken);
            // TODO: Support multiple selectors
            // TODO: Support multiple protocols
            var selector = _selectors[0];
            // TODO: Maybe need a TrySelectProtocol? Or is null good enough?
            var selectedProtocol = await selector.ListenProtocolAsync(pipeline, protocols, cancellationToken)
                .ConfigureAwait(false);
            if (selectedProtocol == null)
            {
                //throw new NotSupportedException($"Protocol {protocol.Identifier} not supported");
                //pipeline.Close();
                // TODO: Accept should probably loop: log the failure to select/negotiate protocol, and wait to try again 
                return (null, null);
            }

            return (pipeline, selectedProtocol);
        }

        public void Dispose()
        {
        }
    }
}
