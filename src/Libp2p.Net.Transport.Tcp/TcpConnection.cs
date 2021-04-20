using System.IO.Pipelines;
using System.Net.Sockets;
using Multiformats.Net;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpConnection : ITransportConnection
    {
        private readonly TcpClient _tcpClient;

        internal TcpConnection(MultiAddress? localAddress, MultiAddress remoteAddress, Direction direction, TcpClient tcpClient)
        {
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
            Direction = direction;
            _tcpClient = tcpClient;
            var stream = _tcpClient.GetStream();
            Input = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
            Output = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public Direction Direction { get; }
        
        public MultiAddress? LocalAddress { get; }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
        
        public MultiAddress RemoteAddress { get; }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
            _tcpClient.Dispose();
        }
    }
}
