using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IProtocol
    {
        string Identifier { get; }
    }
}
