using System;
using System.IO.Pipelines;
using System.Threading;

namespace Libp2p.Net.Streams
{
    public class MplexPipeline : IPipeline, IDisposable
    {
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        internal MplexPipeline(Direction direction, int streamId)
        {
            Direction = direction;
            StreamId = streamId;
            Id = $"{direction}-{streamId}";
        }

        public Direction Direction { get; }
        
        public string Id { get; }
        
        public PipeReader Input => UpstreamPipe.Reader;
        
        public PipeWriter Output => DownstreamPipe.Writer;
        
        public int StreamId { get; }

        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        public void Dispose()
        {
            _stoppingCts.Dispose();
        }
    }
}
