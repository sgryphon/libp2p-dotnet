using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Streams
{
    // See: https://github.com/libp2p/specs/tree/master/mplex
    public class Mplex67 : IMultiplexProtocol
    {
        internal static readonly DiagnosticSource s_diagnosticSource =
            new DiagnosticListener("Libp2p.Net.Streams.Mplex67");

        internal static long MaximumMessageSizeBytes = 1_048_576; // 1 MiB
        
        public string Identifier => "/mplex/6.7.0";

        public async Task<IMultiplexer> StartMultiplexerAsync(IPipeline pipeline, ITransportConnection transportConnection,
            CancellationToken cancellationToken = default)
        {
            var multiplexer = new MplexMultiplexer(pipeline, transportConnection.RemoteAddress);
            await multiplexer.StartAsync(cancellationToken);
            return multiplexer;
        }

        internal static class Diagnostics
        {
            public const string BytesRead = "Mplex67.BytesRead";
            public const string BytesWritten = "Mplex67.BytesWritten";
            public const string Exception = "Mplex67.Exception";
            public const string ConnectionRead = "Mplex67.ConnectionRead";
            public const string ProcessMessage = "Mplex67.ProcessMessage";
            public const string InnerRead = "Mplex67.InnerRead";
            public const string UnknownStream = "Mplex67.UnknownStream";
        }
    }
}
