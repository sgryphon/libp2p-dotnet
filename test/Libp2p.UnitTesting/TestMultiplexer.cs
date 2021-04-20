using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.UnitTesting
{
    public class TestMultiplexer : IMultiplexer
    {
        private readonly IPipeline _pipeline;

        public TestMultiplexer(IPipeline pipeline)
        {
            _pipeline = pipeline;
        }
        
        public Task<IPipeline> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pipeline);
        }

        public Task<IPipeline> AcceptAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_pipeline);
        }
    }
}
