using System.IO.Pipelines;
using Multiformats.Net;

namespace Libp2p.Net.Transport
{
    public class PipeConnection : IConnection
    {
        public PipeConnection(Direction direction, MultiAddress address, PipeReader input, PipeWriter output)
        {
            Direction = direction;
            RemoteAddress = address;
            Input = input;
            Output = output;
        }

        public Direction Direction { get; }
        public PipeReader Input { get; }
        public PipeWriter Output { get; }
        public MultiAddress RemoteAddress { get; }

        public void Dispose()
        {
        }
    }
}
