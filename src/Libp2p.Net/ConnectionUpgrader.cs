using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public class ConnectionUpgrader : IConnectionUpgrader
    {
        private readonly List<IProtocolSelect> _selectors = new List<IProtocolSelect>();
        private readonly List<IEncryptionProtocol> _encryptors = new List<IEncryptionProtocol>();
        private readonly List<IMultiplexProtocol> _multiplexers = new List<IMultiplexProtocol>();

        public ConnectionUpgrader(IList<IProtocolSelect> selectors, IList<IEncryptionProtocol> encryptors, IList<IMultiplexProtocol> multiplexers)
        {
            _selectors.AddRange(selectors);
            _encryptors.AddRange(encryptors);
            _multiplexers.AddRange(multiplexers);
        }

        public async Task<IConnection> UpgradeAsync(ITransportConnection transportConnection, CancellationToken cancellationToken = default)
        {
            // TODO: Support multiple selectors
            var selector = _selectors[0];

            IEncryptionProtocol? encryptionProtocol;
            if (transportConnection.Direction == Direction.Outbound)
            {
                // TODO: Try more than one desired protocol, i.e. fallback to others
                var desiredEncryptor = _encryptors[0];
                encryptionProtocol = await selector
                    .SelectProtocolAsync<IEncryptionProtocol>(transportConnection, desiredEncryptor, cancellationToken)
                    .ConfigureAwait(false);
                if (encryptionProtocol == null)
                {
                    throw new NotSupportedException($"Encryption protocol {desiredEncryptor} not supported");
                }
            }
            else // Inbound
            {
                encryptionProtocol = await selector
                    .ListenProtocolAsync<IEncryptionProtocol>(transportConnection, _encryptors, cancellationToken)
                    .ConfigureAwait(false);
                if (encryptionProtocol == null)
                {
                    throw new NotSupportedException($"No supported encryption protocol");
                }
            }

            var encryptedPipeline = await encryptionProtocol
                    .StartEncryptionAsync(transportConnection, cancellationToken).ConfigureAwait(false);

            IMultiplexProtocol? multiplexProtocol;
            if (transportConnection.Direction == Direction.Outbound)
            {
                var desiredMultiplexer = _multiplexers[0];
                multiplexProtocol = await selector
                    .SelectProtocolAsync(encryptedPipeline, desiredMultiplexer, cancellationToken)
                    .ConfigureAwait(false);
                if (multiplexProtocol == null)
                {
                    throw new NotSupportedException($"Multiplexer protocol {desiredMultiplexer} not supported");
                }
            }
            else
            {
                multiplexProtocol = await selector
                    .ListenProtocolAsync(encryptedPipeline, _multiplexers, cancellationToken).ConfigureAwait(false);
                if (multiplexProtocol == null)
                {
                    throw new NotSupportedException($"No supported multiplexer protocol");
                }
            }

            var multiplexer = await multiplexProtocol.StartMultiplexerAsync(encryptedPipeline, cancellationToken)
                    .ConfigureAwait(false);

            var connection = new Connection(transportConnection, encryptionProtocol, multiplexProtocol, multiplexer, _selectors);

            return connection;
        }
    }
}
