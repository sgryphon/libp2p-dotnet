using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Libp2p.Net
{
    public interface IProtocolSelect : IProtocol
    {
        void Add(string identifier, IProtocol protocol);
    }
}
