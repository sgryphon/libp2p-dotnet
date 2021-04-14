using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    // See: https://github.com/libp2p/specs/tree/master/mplex
    public class Mplex67 : IMultiplexProtocol
    {
        internal static readonly DiagnosticSource s_diagnosticSource =
            new DiagnosticListener("Libp2p.Net.Streams.Mplex67");

        public string Name { get; } = "Mplex 6.7.0";

        public async Task<IMultiplexer> StartMultiplexerAsync(IConnection connection,
            CancellationToken cancellationToken = default)
        {
            var multiplexer = new MplexMultiplexer(connection);
            await multiplexer.StartAsync(cancellationToken);
            return multiplexer;
        }

        internal static class Diagnostics
        {
            public const string Exception = "Mplex67.Exception";
            public const string UnknownStream = "Mplex67.UnknownStream";
        }
    }
}
