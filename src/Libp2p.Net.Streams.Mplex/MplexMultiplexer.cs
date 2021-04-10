using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    public class MplexMultiplexer : IMultiplexer
    {
        private int _nextStreamId = 0;
        private IConnection? _innerConnection;
        private IDictionary<int, MplexConnection> _connections = new Dictionary<int, MplexConnection>();
        private IDictionary<int, Task> _connectionReaderTasks = new Dictionary<int, Task>();

        private SemaphoreSlim _innerConnectionOutputWriteLock = new SemaphoreSlim(1, 1);
        //private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        //private Task? _executingTask;

        public MplexMultiplexer(IConnection innerConnection)
        {
            _innerConnection = innerConnection;
        }
        
        public async Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            var streamId = Interlocked.Increment(ref _nextStreamId);
            var connection = new MplexConnection(streamId);
            _connections[streamId] = connection;
            await StartConnectionAsync(connection, cancellationToken);
            return connection;
        }

        private async Task StartConnectionAsync(MplexConnection connection,
            CancellationToken cancellationToken = default)
        {
            // TODO: Diagnostic activity to create/start connection & send header
            // Send header
            var newStreamHeader = connection.StreamId << 3;
            var newStreamNameBytes = new byte[0];
            await SemaphoreWriteMessageAsync(newStreamHeader, newStreamNameBytes);
            
            // Start upstream connection reader
            var connectionReaderTask = ExecuteUpstreamConnectionReaderAsync(connection);
            _connectionReaderTasks[connection.StreamId] = connectionReaderTask;
        }

        private async Task ForwardBufferDownstreamAsync (MplexConnection connection, ReadOnlySequence<byte> buffer)
        {
            var header = (connection.StreamId << 3) & (connection.IsInitiator ? 0x2 : 0x1);
            foreach (var segment in buffer)
            {
                await SemaphoreWriteMessageAsync(header, segment);
            }
        }

        private async Task SemaphoreWriteMessageAsync(int header, ReadOnlyMemory<byte> segment)
        {
            await _innerConnectionOutputWriteLock.WaitAsync();
            try
            {
                var headerFlush = await _innerConnection!.Output.WriteVarIntAsync(header);
                var lengthFlush = await _innerConnection.Output.WriteVarIntAsync(segment.Length);
                var segmentFlush = await _innerConnection.Output.WriteAsync(segment);
            }
            finally
            {
                _innerConnectionOutputWriteLock.Release();
            }
        }

        private async Task ExecuteUpstreamConnectionReaderAsync(MplexConnection connection)
        {
            try
            {
                var connectionReader = connection.DownstreamPipe.Reader;
                var cancellationToken = connection.StoppingToken;
                while (true)
                {
                    // TODO: Diagnostic activity for each buffer received
                    var result = await connectionReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (buffer.Length > 0)
                    {
                        await ForwardBufferDownstreamAsync(connection, buffer);
                    }
                    connection.Input.AdvanceTo(buffer.End, buffer.End);
                }
            }
            catch (Exception ex)
            {
                if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.Exception))
                {
                    Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.Exception, ex);
                }
            }
        }
        
        // TODO: Start listening

        /*
        internal Task StartAsync( CancellationToken cancellationToken = default)
        {
            _executingTask = ExecuteConnectionReaderAsync();
            if (_executingTask.IsCompleted)
            {
                // Bubble any cancellation or failure
                return _executingTask;
            }

            return Task.CompletedTask;
        }
        */
    }
}
