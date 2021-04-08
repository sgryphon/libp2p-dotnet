using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Cryptography
{
    public class Plaintext : IProtocol
    {
        public string Name => "Plaintext";
        
        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
