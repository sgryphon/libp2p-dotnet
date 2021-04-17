using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocolSelect<T> where T: class
    {
        void Add(string identifier, T protocol);
        Task<T?> SelectProtocolAsync(IPipeline pipeline, CancellationToken cancellationToken = default);
    }
}
