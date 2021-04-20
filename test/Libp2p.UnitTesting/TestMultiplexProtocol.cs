using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.UnitTesting
{
    public class TestMultiplexProtocol : IMultiplexProtocol
    {
        public TestMultiplexProtocol(string identifier)
        {
            Identifier = identifier;
        }
        
        public string Identifier { get; }
        
        public Task<IMultiplexer> StartMultiplexerAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            var testMultiplexer = new TestMultiplexer(pipeline);
            return Task.FromResult<IMultiplexer>(testMultiplexer);
        }
    }
}
