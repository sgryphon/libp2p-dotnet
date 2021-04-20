using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public static class ConnectionExtensions
    {
        public static async Task<(IPipeline?, string?)> ConnectAsync(this IConnection connection, string protocolIdentifier, CancellationToken cancellationToken = default)
        {
            var (pipeline, protocol) = await connection.ConnectAsync(new ProtocolIdentifier(protocolIdentifier), cancellationToken);
            return (pipeline, protocol?.Identifier);
        }

        public static async Task<(IPipeline, string)> AcceptAsync(this IConnection connection, IList<string> protocolIdentifiers, CancellationToken cancellationToken = default)
        {
            var protocols = protocolIdentifiers.Select(x => new ProtocolIdentifier(x)).ToList<IProtocol>();
            var (pipeline, protocol) = await connection.AcceptAsync(protocols, cancellationToken);
            return (pipeline, protocol?.Identifier);
        }

        class ProtocolIdentifier : IProtocol
        {
            public ProtocolIdentifier(string identifer)
            {
                Identifier = identifer;
            }
            
            public string Identifier { get; }
        }
    }
}
