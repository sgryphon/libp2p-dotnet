using System;
using System.Threading;
using System.Threading.Tasks;
using Example.Protocol;
using Libp2p.Net;
using Libp2p.Net.Cryptography;
using Libp2p.Net.Discovery;
using Libp2p.Net.Protocol;
using Libp2p.Net.Streams;
using Libp2p.Net.Transport.Tcp;
using Libp2p.Peering;
using Microsoft.Extensions.Configuration;

namespace Example.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CTRL+C to exit");
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args)
                .Build();
            
            var minimumDesired = configuration.GetValue<int>("Discovery:MinimumDesired");
            var bootstrapAddresses = configuration.GetValue<string[]>("Discovery:Discovery");
            
            var transports = new ITransport[] {new TcpTransport()};
            var selectors = new IProtocolSelect[] {new MultistreamSelect1()};
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var discovery = new IDiscovery[] {new BootstrapDiscovery(bootstrapAddresses)};

            var statusProtocol = new StatusProtocol();
            var beaconBlocksByRange = new BeaconBlocksByRangeProtocol();
            
            var peerPool = new PeerPool();
            peerPool.ConfigureConnect(transports, selectors, encryptors, multiplexers);
            peerPool.ConfigureListen(transports, selectors, encryptors, multiplexers);
            
            peerPool.AddDiscovery(discovery);
            peerPool.MinimumDesired = configuration.GetValue<int>("Discovery:MinimumDesired");

            var cts = new CancellationTokenSource();
            await peerPool.StartAsync(cts.Token);

            while (true)
            {
                var connection = peerPool.AcceptConnectionAsync(cts.Token);
                if (connection.Direction == Direction.Outbound)
                {
                    // A new outbound connection should initiate Status
                    await statusProtocol.StartAsync(connection, cts.Token);
                }
            }
            
            //LOOP:
            //peerPool.AcceptConnection() => stick in a Channel (for multithreaded async processing)
            // ... some you dialled, some were dialled by others
            // ... maybe want to just grab a random connection from pool, e.g. for gossip

            // new connection (that we dialled) => open multiplex stream, send status
            // ALSO new connection => listen for response on stream
            // when status received (either on dialled or received), compare,
            // => if less, open new multiplex stream and send beacon block range request

        }
    }
}
