using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Libp2p.Net.Streams
{
    public class MplexMultiplexer : IMultiplexer
    {
        private readonly IDictionary<(bool, int), Task> _connectionReaderTasks = new Dictionary<(bool, int), Task>();
        private readonly IDictionary<(bool, int), MplexConnection> _connections =
            new Dictionary<(bool, int), MplexConnection>();
        private readonly IConnection? _innerConnection;
        private readonly SemaphoreSlim _innerConnectionOutputWriteLock = new SemaphoreSlim(1, 1);
        private Task? _innerConnectionReaderTask;
        private int _nextStreamId;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private Channel<MplexConnection> _connectionsReceived = Channel.CreateUnbounded<MplexConnection>();

        public MplexMultiplexer(IConnection innerConnection)
        {
            _innerConnection = innerConnection;
        }

        public async Task<IConnection> ConnectAsync(CancellationToken cancellationToken = default)
        {
            var streamId = Interlocked.Increment(ref _nextStreamId);
            var connection = new MplexConnection(true, streamId);
            await StartConnectionAsync(connection, cancellationToken);
            return connection;
        }

        private async Task ExecuteInnerConnectionReaderAsync()
        {
            try
            {
                var connectionReader = _innerConnection!.Input;
                var cancellationToken = _stoppingCts.Token;
                while (true)
                {
                    // TODO: Diagnostic activity for each buffer received
                    var result = await connectionReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    var consumed = await ProcessMessages(buffer, cancellationToken).ConfigureAwait(false);
                    _innerConnection.Input.AdvanceTo(buffer.GetPosition(consumed), buffer.End);
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

        private async Task<long> ProcessMessages(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var consumed = 0L;
            while (true)
            {
                if (!buffer.TryReadVarInt(out var headerConsumed, out var header))
                {
                    break;
                }

                if (!buffer.Slice(headerConsumed).TryReadVarInt(out var lengthConsumed, out var length))
                {
                    break;
                }

                var totalLength = headerConsumed + lengthConsumed + length;
                if (buffer.Length < totalLength)
                {
                    break;
                }

                await ProcessMessage(header, buffer.Slice(headerConsumed + lengthConsumed, length), cancellationToken)
                    .ConfigureAwait(false);
                
                buffer = buffer.Slice(totalLength);
                consumed += totalLength;
            }

            return consumed;
        }

        private async Task ProcessMessage(int header, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var messageFlag = (MessageFlag)(header & 0x7);
            var streamId = header >> 3;
            switch (messageFlag)
            {
                case MessageFlag.NewStream:
                    await ReceiveNewStreamAsync(streamId, cancellationToken).ConfigureAwait(false);
                    break;
                case MessageFlag.MessageReceiver:
                case MessageFlag.MessageInitiator:
                    await WriteUpstreamMessageAsync(messageFlag, streamId, buffer, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new NotImplementedException($"Mplex message type {messageFlag} not implemented yet.");
            }
        }

        private async Task ReceiveNewStreamAsync(int streamId, CancellationToken cancellationToken)
        {
            var newConnection = new MplexConnection(false, streamId);
            await StartConnectionAsync(newConnection, cancellationToken);
            await _connectionsReceived.Writer.WriteAsync(newConnection, cancellationToken);
        }

        private ValueTask<FlushResult> WriteUpstreamMessageAsync(MessageFlag messageFlag, int streamId,
            ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var key = (messageFlag == MessageFlag.MessageInitiator, streamId);
            if (!_connections.TryGetValue(key, out var connection))
            {
                if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.UnknownStream))
                {
                    Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.UnknownStream, new {
                        Key = key
                    });
                }

                return new ValueTask<FlushResult>(new FlushResult());
            }
            
            var upstreamBuffer = connection.UpstreamPipe.Writer.GetSpan((int)buffer.Length);

            var index = 0;
            foreach (var segment in buffer)
            {
                segment.Span.CopyTo(upstreamBuffer[index..]);
                index += segment.Length;
            }

            connection.UpstreamPipe.Writer.Advance(index);
            return connection.UpstreamPipe.Writer.FlushAsync(cancellationToken);
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

                    connectionReader.AdvanceTo(buffer.End, buffer.End);
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

        private async Task SemaphoreWriteDownstreamMessageAsync(int header, ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken = default)
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

        private async Task SemaphoreWriteDownstreamMessageAsync(int header, ReadOnlyMemory<byte> segment,
            CancellationToken cancellationToken = default)
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

        // TODO: Start listening

        internal Task StartAsync(CancellationToken cancellationToken = default)
        {
            _innerConnectionReaderTask = ExecuteInnerConnectionReaderAsync();
            if (_innerConnectionReaderTask.IsCompleted)
            {
                // Bubble any cancellation or failure
                return _innerConnectionReaderTask;
            }

            return Task.CompletedTask;
        }

        private async Task StartConnectionAsync(MplexConnection connection,
            CancellationToken cancellationToken = default)
        {
            // TODO: Diagnostic activity to create/start connection & send header
            _connections[(connection.IsInitiator, connection.StreamId)] = connection;
            
            if (connection.IsInitiator)
            {
                // Send header
                var newStreamHeader = connection.StreamId << 3;
                var newStreamNameBytes = new byte[0];
                await SemaphoreWriteDownstreamMessageAsync(newStreamHeader, newStreamNameBytes, cancellationToken);
            }

            // Start upstream connection reader
            var connectionReaderTask = ExecuteUpstreamConnectionReaderAsync(connection);
            _connectionReaderTasks[(connection.IsInitiator, connection.StreamId)] = connectionReaderTask;
        }

        public void Dispose()
        {
        }

        public async Task<IConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            var newConnection = await _connectionsReceived.Reader.ReadAsync(cancellationToken);
            return newConnection;
        }
    }
}
