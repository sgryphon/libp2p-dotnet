using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;
using Multiformats.Net;

namespace Libp2p.Peering
{
    public class Connection : IConnection
    {
        private readonly IMultiplexer _multiplexer;

        public Connection(IMultiplexer multiplexer)
        {
            _multiplexer = multiplexer;
        }
        
        public Direction Direction { get; }
        public MultiAddress RemoteAddress { get; }
        public IMultiplexProtocol MultiplexProtocol { get; }
        public IEncryptionProtocol EncryptionProtocol { get; }

        public async Task<(IPipeline, IProtocol)> ConnectAsync(IProtocol protocol, CancellationToken cancellationToken = default)
        {
            var pipeline = await _multiplexer.ConnectAsync(cancellationToken);
            return (pipeline, null);
        }

        public async Task<(IPipeline, IProtocol)> AcceptAsync(IList<IProtocol> protocol, CancellationToken cancellationToken = default)
        {
            var pipeline = await _multiplexer.AcceptAsync(cancellationToken);
            return (pipeline, null);
        }
    }
}
