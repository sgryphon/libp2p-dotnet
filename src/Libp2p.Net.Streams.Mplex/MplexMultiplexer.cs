using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    public class MplexMultiplexer : IMultiplexer
    {
        private int _nextStreamId;
        private readonly IConnection? _innerConnection;
        private readonly IDictionary<int, MplexConnection> _connections = new Dictionary<int, MplexConnection>();
        private readonly IDictionary<int, Task> _connectionReaderTasks = new Dictionary<int, Task>();

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
            await SemaphoreWriteDownstreamMessageAsync(newStreamHeader, newStreamNameBytes, cancellationToken);
            
            // Start upstream connection reader
            var connectionReaderTask = ExecuteUpstreamConnectionReaderAsync(connection);
            _connectionReaderTasks[connection.StreamId] = connectionReaderTask;
        }

        private async Task SemaphoreWriteDownstreamMessageAsync (int header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken = default)
        {
            // TODO: Message size limits for Mplex
            await _innerConnectionOutputWriteLock.WaitAsync(cancellationToken);
            try
            {
                var memory = _innerConnection!.Output.GetMemory((int)buffer.Length + 5 + 5);
                VarIntUtility.WriteVarInt(memory.Span, header, out var headerBytesWritten);
                VarIntUtility.WriteVarInt(memory.Span.Slice(headerBytesWritten), (int)buffer.Length,
                    out var lengthBytesWritten);
                var index = headerBytesWritten + lengthBytesWritten;
                foreach (var segment in buffer)
                {
                    segment.CopyTo(memory.Slice(index));
                    index += segment.Length;
                }
                _innerConnection.Output.Advance(index);
                _ = await _innerConnection.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _innerConnectionOutputWriteLock.Release();
            }
        }

        private async Task SemaphoreWriteDownstreamMessageAsync(int header, ReadOnlyMemory<byte> segment, CancellationToken cancellationToken = default)
        {
            await _innerConnectionOutputWriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var memory = _innerConnection!.Output.GetMemory(segment.Length + 5 + 5);
                VarIntUtility.WriteVarInt(memory.Span, header, out var headerBytesWritten);
                VarIntUtility.WriteVarInt(memory.Slice(headerBytesWritten).Span, segment.Length,
                    out var lengthBytesWritten);
                segment.CopyTo(memory.Slice(headerBytesWritten + lengthBytesWritten));
                _innerConnection.Output.Advance(headerBytesWritten + lengthBytesWritten + segment.Length);
                _ = await _innerConnection.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
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
                        var header = (connection.StreamId << 3) | (connection.IsInitiator ? 0x1 : 0x2);
                        await SemaphoreWriteDownstreamMessageAsync(header, buffer, cancellationToken);
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
