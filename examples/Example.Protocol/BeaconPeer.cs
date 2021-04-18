using Libp2p.Net;

namespace Example.Protocol
{
    public class BeaconPeer
    {
        public IConnection? Connection { get; set; }
        public int? HeadSlot { get; set; }
    }
}
