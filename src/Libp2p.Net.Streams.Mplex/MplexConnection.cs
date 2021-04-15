using System.IO.Pipelines;
using System.Threading;
using Multiformats.Net;

namespace Libp2p.Net.Streams
{
    public class MplexConnection : IConnection
    {
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        internal MplexConnection(Direction direction, MultiAddress address, int streamId)
        {
            Direction = direction;
            RemoteAddress = address;
            StreamId = streamId;
            Id = $"{direction}-{streamId}";
        }

        public Direction Direction { get; }
        
        public string Id { get; }
        
        public PipeReader Input => UpstreamPipe.Reader;
        
        public PipeWriter Output => DownstreamPipe.Writer;
        
        public MultiAddress RemoteAddress { get; }
        
        public int StreamId { get; }

        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        public void Dispose()
        {
        }
    }
}
