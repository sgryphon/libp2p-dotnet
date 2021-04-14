namespace Libp2p.Net.Streams
{
    public enum MessageFlag
    {
        NewStream = 0,
        MessageReceiver = 1,
        MessageInitiator = 2,
        CloseReceiver = 3,
        CloseInitiator = 4,
        ResetReceiver = 5,
        ResetInitiator = 6
    }
}
