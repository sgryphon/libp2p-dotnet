using System.IO.Pipelines;
using Multiformats.Net;

namespace Libp2p.Net.Transport
{
    public class PipeConnection : IConnection
    {
        public PipeConnection(MultiAddress address, PipeReader input, PipeWriter output)
        {
            RemoteAddress = address;
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        public void Dispose()
        {
        }

        public MultiAddress RemoteAddress { get; }
    }
}
