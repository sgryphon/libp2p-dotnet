using System;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    // See: https://github.com/libp2p/specs/tree/master/mplex
    public class Mplex67 : IProtocolMultiplex
    {
        public string Name => "Mplex 6.7.0";
        
        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IConnectionListener> ListenAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
