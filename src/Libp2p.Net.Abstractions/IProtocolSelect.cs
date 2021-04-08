namespace Libp2p.Net
{
    public interface IProtocolSelect : IProtocol
    {
        void Add(string identifier, IProtocol protocol);
    }
}
