using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Cryptography
{
    public class Plaintext : IEncryptionProtocol
    {
        public string Name => "Plaintext";

        public Task<IPipeline> StartEncryptionAsync(IPipeline pipeline,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(pipeline);
        }
    }
}
