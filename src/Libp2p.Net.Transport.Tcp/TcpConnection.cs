using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace Libp2p.Net.Transport.Tcp
{
    internal class TcpConnection : IConnection, IDuplexPipe
    {
        private readonly TcpClient _tcpClient;

        internal TcpConnection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            var stream = _tcpClient.GetStream(); 
            Input = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
            Output = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
            _tcpClient.Dispose();
        }

        public PipeReader Input { get; }
        
        public PipeWriter Output { get; }
    }
}
