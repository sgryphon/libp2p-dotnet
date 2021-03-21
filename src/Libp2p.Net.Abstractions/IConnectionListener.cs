using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IConnectionListener
    {
        Task<IConnection> AcceptConnectionAsync();
    }
}
