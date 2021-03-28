namespace Libp2p.Net
{
    public interface IProtocol
    {
        void Start(IConnection connection);
    }
}
