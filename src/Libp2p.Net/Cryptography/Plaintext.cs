using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Cryptography
{
    public class Plaintext : IEncryptionProtocol
    {
        public string Name => "Plaintext";

        public Task<IConnection> StartEncryptionAsync(IConnection connection,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(connection);
        }
    }
}
