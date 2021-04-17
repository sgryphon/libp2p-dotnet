using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    // dotnet run --project examples/Example.Client
    class Program
    {
        private static List<IDisposable> _allListeners = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("CTRL+C to exit");
            
            SubscribeToAllLibp2pDiagnostics();
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddCommandLine(args)
                .Build();
            
            var minimumDesired = configuration.GetValue<int>("Discovery:MinimumDesired");
            var bootstrapAddresses = configuration.GetSection("Discovery:Discovery").GetChildren().Select(x => x.Value)
                .ToArray();
            
            var transports = new ITransport[] {new TcpTransport()};
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var encryptorSelectors = new IProtocolSelect<IEncryptionProtocol>[] {new MultistreamSelect1<IEncryptionProtocol>
            {
                ["/plaintext/1.0.0"] = new Plaintext()
            }};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var multiplexerSelectors = new IProtocolSelect<IMultiplexProtocol>[] {new MultistreamSelect1<IMultiplexProtocol>
            {
                ["/mplex/6.7.0"] = new Mplex67()
            }};
            var discovery = new IDiscovery[] {new BootstrapDiscovery(bootstrapAddresses)};

            var statusProtocol = new StatusProtocol();
            var beaconBlocksByRange = new BeaconBlocksByRangeProtocol();
            
            var peerPool = new PeerPool();
            peerPool.ConfigureConnect(transports, encryptors, multiplexers);
            peerPool.ConfigureListen(transports, encryptors, multiplexers);
            peerPool.AddDiscovery(discovery);
            peerPool.MinimumDesired = minimumDesired;

            var cts = new CancellationTokenSource();
            await peerPool.StartAsync(cts.Token);

            while (true)
            {
                // Should this be AcceptMultiplexer ??
                // Or look at node library Connection (upgraded, i.e. is a multiplexer) vs MultiAddressConnection
                // Would it make sense to be similar to the node API ?
                // https://github.com/libp2p/js-libp2p-interfaces
                var connection = await peerPool.AcceptConnectionAsync(cts.Token);
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
        
        private static void SubscribeToAllLibp2pDiagnostics()
        {
            _allListeners.Add(DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                if (listener.Name.StartsWith("Libp2p."))
                {
                    _allListeners.Add(listener.Subscribe(kvp =>
                    {
                        if (kvp.Value is Activity activity)
                        {
                            if (kvp.Key.EndsWith(".Start"))
                            {
                                Console.WriteLine("{0}: {1} {2}-{3} {4}", kvp.Key,
                                    activity.OperationName,
                                    activity.TraceId, activity.SpanId, string.Concat(activity.Tags));
                            }
                            else
                            {
                                Console.WriteLine("{0}: {1}({2}) {3}-{4} {5}", kvp.Key,
                                    activity.OperationName, activity.Duration,
                                    activity.TraceId, activity.SpanId, string.Concat(activity.Tags));
                            }
                        }
                        else
                        {
                            var currentActivity = Activity.Current;
                            Console.WriteLine("{0}: {1} {2}-{3}", kvp.Key, kvp.Value, 
                                currentActivity?.TraceId, currentActivity?.SpanId);
                        }
                    }));
                }
            }));
        }

    }
}
