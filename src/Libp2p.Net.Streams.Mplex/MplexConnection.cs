using System.IO.Pipelines;
using System.Threading;
using Multiformats.Net;

namespace Libp2p.Net.Streams
{
    public class MplexConnection : IConnection
    {
        public string Id { get; }
        public bool IsInitiator { get; }
        public int StreamId { get; }
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        internal MplexConnection(MultiAddress address, bool isInitiator, int streamId)
        {
            RemoteAddress = address;
            IsInitiator = isInitiator;
            StreamId = streamId;
            Id = string.Format(isInitiator ? "Initiator-{0}" : "Receiver-{0}", streamId);
        }

        public PipeReader Input => UpstreamPipe.Reader;

        public PipeWriter Output => DownstreamPipe.Writer;
        
        internal Pipe DownstreamPipe { get; } = new Pipe();

        internal CancellationToken StoppingToken => _stoppingCts.Token;

        internal Pipe UpstreamPipe { get; } = new Pipe();

        public void Dispose()
        {
        }

        public MultiAddress RemoteAddress { get; }
    }
}
