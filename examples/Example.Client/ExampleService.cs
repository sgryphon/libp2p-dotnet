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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Example.Client
{
    public class ExampleService : BackgroundService
    {
        private List<IDisposable>? _allListeners = new();
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<DiscoverySettings> _discoverySettingsMonitor;
        private ConnectionPool? _connectionPool;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private List<BeaconPeer> _beaconPeers = new List<BeaconPeer>();
        private List<Task> _beaconPeerTasks = new List<Task>();
        private StatusProtocol _statusProtocol = new StatusProtocol();
        private BeaconBlocksByRangeProtocol _beaconBlocksByRangeProtocol = new BeaconBlocksByRangeProtocol();

        public ExampleService(ILogger<ExampleService> logger,
            IOptionsMonitor<DiscoverySettings> discoverySettingsMonitor)
        {
            _logger = logger;
            _discoverySettingsMonitor = discoverySettingsMonitor;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SubscribeToAllLibp2pDiagnostics();

            var discoverySettings = _discoverySettingsMonitor.CurrentValue;
            
            var transports = new ITransport[] {new TcpTransport()};
            var selectors = new IProtocolSelect[] {new MultistreamSelect1()};
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var discovery = new IDiscovery[] {new BootstrapDiscovery(discoverySettings.BootstrapAddresses)};

            _statusProtocol = new StatusProtocol();
            _beaconBlocksByRangeProtocol = new BeaconBlocksByRangeProtocol();

            var peerPool = new PeerPool();
            peerPool.AddDiscovery(discovery);
            await peerPool.StartAsync(_cancellationTokenSource.Token);

            var connectionPool = new ConnectionPool(peerPool);
            connectionPool.Configure(transports, selectors, encryptors, multiplexers);
            connectionPool.MinimumDesired = discoverySettings.MinimumDesired;

            await ProcessNewConnectionAsync(_cancellationTokenSource.Token);

            //LOOP:
            //peerPool.AcceptConnection() => stick in a Channel (for multithreaded async processing)
            // ... some you dialled, some were dialled by others
            // ... maybe want to just grab a random connection from pool, e.g. for gossip

            // new connection (that we dialled) => open multiplex stream, send status
            // ALSO new connection => listen for response on stream
            // when status received (either on dialled or received), compare,
            // => if less, open new multiplex stream and send beacon block range request
        }

        private async Task ProcessNewConnectionAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Would it make sense to be similar to the node API ?
                // https://github.com/libp2p/js-libp2p-interfaces
                
                var connection = await _connectionPool!.AcceptAsync(_cancellationTokenSource.Token);

                var beaconPeer = new BeaconPeer()
                {
                    Connection = connection
                };
                _beaconPeers.Add(beaconPeer);
                var peerAcceptTask = ProcessNewPipelineAsync(connection, _cancellationTokenSource.Token);
                _beaconPeerTasks.Add(peerAcceptTask);
                
                if (connection.Direction == Direction.Outbound)
                {
                    // A new outbound connection should initiate Status
                    var (pipeline, protocol) = await connection.ConnectAsync(_statusProtocol, _cancellationTokenSource.Token); // Need desired protocol
                    await _statusProtocol.StartAsync(pipeline, _cancellationTokenSource.Token);
                }
            }
        }
        
        private async Task ProcessNewPipelineAsync(IConnection connection, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await connection.AcceptAsync(new IProtocol[] {_statusProtocol, _beaconBlocksByRangeProtocol}, _cancellationTokenSource.Token); // Need list of protocols
            }
        }
        
        private void SubscribeToAllLibp2pDiagnostics()
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
