using System.IO.Pipelines;
using System.Threading;

namespace Libp2p.Net.Streams
{
    public class MplexConnection : IConnection
    {
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        public MplexConnection(int streamId)
        {
            StreamId = streamId;
        }

        public PipeReader Input => UpstreamPipe.Reader;

        public PipeWriter Output => DownstreamPipe.Writer;

        public int StreamId { get; }

        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal bool IsInitiator => true;

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        public void Dispose()
        {
        }
    }
}
