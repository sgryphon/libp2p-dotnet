using System.IO.Pipelines;
using Multiformats.Net;

namespace Libp2p.Net.Transport
{
    public class PipeConnection : ITransportConnection
    {
        public PipeConnection(MultiAddress localAddress, MultiAddress remoteAddress, Direction direction, PipeReader input, PipeWriter output)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            Direction = direction;
            Input = input;
            Output = output;
        }

        public Direction Direction { get; }
        public MultiAddress? LocalAddress { get; }
        public PipeReader Input { get; }
        public PipeWriter Output { get; }
        public MultiAddress RemoteAddress { get; }

        public void Dispose()
        {
        }
    }
}
