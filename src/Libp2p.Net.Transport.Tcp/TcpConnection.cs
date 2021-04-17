using System.IO.Pipelines;
using System.Net.Sockets;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpConnection : ITransportConnection
    {
        private readonly TcpClient _tcpClient;

        internal TcpConnection(Direction direction, MultiAddress remoteAddress, TcpClient tcpClient)
        {
            Direction = direction;
            RemoteAddress = remoteAddress;
            _tcpClient = tcpClient;
            var stream = _tcpClient.GetStream();
            Input = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
            Output = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
            _tcpClient.Dispose();
        }

        public Direction Direction { get; }
        public MultiAddress RemoteAddress { get; }
    }
}
