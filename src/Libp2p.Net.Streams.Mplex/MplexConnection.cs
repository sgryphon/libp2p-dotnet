using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    public class MplexConnection : IConnection
    {
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        public MplexConnection(int streamId)
        {
            StreamId = streamId;
        }
        
        public int StreamId { get; }
        
        public void Dispose()
        {
        }

        public PipeReader Input => UpstreamPipe.Reader;

        public PipeWriter Output => DownstreamPipe.Writer;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal bool IsInitiator => true;
    }
}
