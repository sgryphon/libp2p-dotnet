using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IEncryptionProtocol
    {
        string Name { get; }

        Task<IConnection> StartEncryptionAsync(IConnection connection, CancellationToken cancellationToken = default);
    }
}
