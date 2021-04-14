using System.IO.Pipelines;
using System.Threading;

namespace Libp2p.Net.Streams
{
    public class MplexConnection : IConnection
    {
        public bool IsInitiator { get; }
        public int StreamId { get; }
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        public MplexConnection(bool isInitiator, int streamId)
        {
            IsInitiator = isInitiator;
            StreamId = streamId;
        }

        public PipeReader Input => UpstreamPipe.Reader;

        public PipeWriter Output => DownstreamPipe.Writer;
        
        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        public void Dispose()
        {
        }
    }
}
