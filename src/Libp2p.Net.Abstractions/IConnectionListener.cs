using System;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public interface IConnectionListener : IDisposable
    {
        Task<IConnection> AcceptConnectionAsync();
    }
}
