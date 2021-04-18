using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Multiformats.Net;

namespace Libp2p.Net.Streams
{
    public class MplexMultiplexer : IMultiplexer
    {
        private readonly SemaphoreSlim _downstreamOutputWriteLock = new SemaphoreSlim(1, 1);
        private readonly IPipeline _downstreamPipeline;
        private Task? _downstreamReaderTask;
        private readonly Channel<MplexPipeline> _newConnectionsReceived = Channel.CreateUnbounded<MplexPipeline>();
        private int _nextStreamId;
        private readonly MultiAddress _remoteAddress;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        private readonly IDictionary<(Direction, int), MplexPipeline> _upstreamConnections =
            new Dictionary<(Direction, int), MplexPipeline>();

        private readonly IDictionary<(Direction, int), Task> _upstreamReaderTasks =
            new Dictionary<(Direction, int), Task>();

        public MplexMultiplexer(IPipeline downstreamPipeline, MultiAddress remoteAddress)
        {
            _downstreamPipeline = downstreamPipeline;
            _remoteAddress = remoteAddress;
        }

        public async Task<IPipeline> AcceptAsync(CancellationToken cancellationToken = default)
        {
            var newConnection = await _newConnectionsReceived.Reader.ReadAsync(cancellationToken);
            return newConnection;
        }

        public async Task<IPipeline> ConnectAsync(CancellationToken cancellationToken = default)
        {
            var streamId = Interlocked.Increment(ref _nextStreamId);
            var connection = new MplexPipeline(Direction.Outbound, _remoteAddress, streamId);
            await StartConnectionAsync(connection, cancellationToken);
            return connection;
        }

        public void Dispose()
        {
        }

        private async Task ExecuteInnerConnectionReaderAsync()
        {
            try
            {
                var connectionReader = _downstreamPipeline!.Input;
                var cancellationToken = _stoppingCts.Token;
                while (true)
                {
                    var result = await connectionReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var activity = default(Activity);
                    try
                    {
                        if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.InnerRead))
                        {
                            activity = new Activity(Mplex67.Diagnostics.InnerRead);
                            activity = Mplex67.s_diagnosticSource.StartActivity(activity, activity);
                        }

                        var buffer = result.Buffer;
                        var consumed = await ProcessInnerReaderMessages(buffer, cancellationToken)
                            .ConfigureAwait(false);
                        _downstreamPipeline.Input.AdvanceTo(buffer.GetPosition(consumed), buffer.End);
                        if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.BytesRead))
                        {
                            Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.BytesRead, new {BytesRead = consumed});
                        }
                    }
                    finally
                    {
                        if (activity != null)
                        {
                            Mplex67.s_diagnosticSource.StopActivity(activity, activity);
                        }
                    }
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

        private async Task ExecuteUpstreamConnectionReaderAsync(MplexPipeline pipeline)
        {
            try
            {
                var connectionReader = pipeline.DownstreamPipe.Reader;
                var cancellationToken = pipeline.StoppingToken;
                while (true)
                {
                    var result = await connectionReader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var activity = default(Activity);
                    try
                    {
                        if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.ConnectionRead))
                        {
                            activity = new Activity(Mplex67.Diagnostics.ConnectionRead);
                            activity.AddTag("Stream", pipeline.Id);
                            activity.Start();
                            activity = Mplex67.s_diagnosticSource.StartActivity(activity, activity);
                        }

                        var buffer = result.Buffer;
                        if (buffer.Length > 0)
                        {
                            var header = (pipeline.StreamId << 3) |
                                         (int)(pipeline.Direction == Direction.Inbound
                                             ? MessageFlag.MessageReceiver
                                             : MessageFlag.MessageInitiator);
                            var chunkSize = Mplex67.MaximumMessageSizeBytes - 10;
                            while (buffer.Length > chunkSize)
                            {
                                await SemaphoreWriteDownstreamMessageAsync(header, buffer.Slice(0, chunkSize),
                                    cancellationToken).ConfigureAwait(false);
                                buffer = buffer.Slice(chunkSize);
                            }

                            await SemaphoreWriteDownstreamMessageAsync(header, buffer, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        connectionReader.AdvanceTo(buffer.End, buffer.End);
                    }
                    finally
                    {
                        if (activity != null)
                        {
                            Mplex67.s_diagnosticSource.StopActivity(activity, activity);
                        }
                    }
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

        private async Task<long> ProcessInnerReaderMessages(ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            var consumed = 0L;
            while (true)
            {
                // NOTE: Header should be a base128 varint (we are only using effective base31)
                if (!buffer.TryReadVarInt(out var headerConsumed, out var header))
                {
                    break;
                }

                if (!buffer.Slice(headerConsumed).TryReadVarInt(out var lengthConsumed, out var length))
                {
                    break;
                }

                // TODO: Check length > 1 MiB (protocol violation)

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

        private async Task ProcessMessage(int header, ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            var activity = default(Activity);
            try
            {
                var messageFlag = (MessageFlag)(header & 0x7);
                var streamId = header >> 3;
                if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.ProcessMessage))
                {
                    activity = new Activity(Mplex67.Diagnostics.ProcessMessage);
                    activity.AddTag("Flag", messageFlag.ToString());
                    activity.AddTag("StreamId", streamId.ToString(CultureInfo.InvariantCulture));
                    activity = Mplex67.s_diagnosticSource.StartActivity(activity, activity);
                }

                switch (messageFlag)
                {
                    case MessageFlag.NewStream:
                        await ReceiveNewStreamAsync(streamId, cancellationToken).ConfigureAwait(false);
                        break;
                    case MessageFlag.MessageReceiver:
                    case MessageFlag.MessageInitiator:
                        await WriteUpstreamMessageAsync(messageFlag, streamId, buffer, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    default:
                        throw new NotImplementedException($"Mplex message type {messageFlag} not implemented yet.");
                }
            }
            finally
            {
                if (activity != null)
                {
                    Mplex67.s_diagnosticSource.StopActivity(activity, activity);
                }
            }
        }

        private async Task ReceiveNewStreamAsync(int streamId, CancellationToken cancellationToken)
        {
            var newConnection = new MplexPipeline(Direction.Inbound, _remoteAddress, streamId);
            await StartConnectionAsync(newConnection, cancellationToken);
            await _newConnectionsReceived.Writer.WriteAsync(newConnection, cancellationToken);
        }

        private async Task SemaphoreWriteDownstreamMessageAsync(int header, ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (buffer.Length > Mplex67.MaximumMessageSizeBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer), "Message size exceeds 1 MiB limit");
            }

            int bytesWritten;
            await _downstreamOutputWriteLock.WaitAsync(cancellationToken);
            try
            {
                var memory = _downstreamPipeline!.Output.GetMemory((int)buffer.Length + 5 + 5);
                VarIntUtility.WriteVarInt(memory.Span, header, out var headerBytesWritten);
                VarIntUtility.WriteVarInt(memory.Span.Slice(headerBytesWritten), (int)buffer.Length,
                    out var lengthBytesWritten);
                var index = headerBytesWritten + lengthBytesWritten;
                foreach (var segment in buffer)
                {
                    segment.CopyTo(memory.Slice(index));
                    index += segment.Length;
                }

                bytesWritten = index;
                _downstreamPipeline.Output.Advance(bytesWritten);
                _ = await _downstreamPipeline.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _downstreamOutputWriteLock.Release();
            }

            if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.BytesWritten))
            {
                Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.BytesWritten, new {BytesWritten = bytesWritten});
            }
        }

        private async Task SemaphoreWriteDownstreamMessageAsync(int header, ReadOnlyMemory<byte> segment,
            CancellationToken cancellationToken = default)
        {
            if (segment.Length > Mplex67.MaximumMessageSizeBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(segment), "Message size exceeds 1 MiB limit");
            }

            int bytesWritten;
            await _downstreamOutputWriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var memory = _downstreamPipeline!.Output.GetMemory(segment.Length + 5 + 5);
                VarIntUtility.WriteVarInt(memory.Span, header, out var headerBytesWritten);
                VarIntUtility.WriteVarInt(memory.Slice(headerBytesWritten).Span, segment.Length,
                    out var lengthBytesWritten);
                segment.CopyTo(memory.Slice(headerBytesWritten + lengthBytesWritten));
                bytesWritten = headerBytesWritten + lengthBytesWritten + segment.Length;
                _downstreamPipeline.Output.Advance(bytesWritten);
                _ = await _downstreamPipeline.Output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _downstreamOutputWriteLock.Release();
            }

            if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.BytesWritten))
            {
                Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.BytesWritten, new {BytesWritten = bytesWritten});
            }
        }

        internal Task StartAsync(CancellationToken cancellationToken = default)
        {
            _downstreamReaderTask = ExecuteInnerConnectionReaderAsync();
            if (_downstreamReaderTask.IsCompleted)
            {
                // Bubble any cancellation or failure
                return _downstreamReaderTask;
            }

            return Task.CompletedTask;
        }

        private async Task StartConnectionAsync(MplexPipeline pipeline,
            CancellationToken cancellationToken = default)
        {
            // TODO: Diagnostic activity to create/start connection & send header
            _upstreamConnections[(pipeline.Direction, pipeline.StreamId)] = pipeline;

            if (pipeline.Direction == Direction.Outbound)
            {
                // Send header
                var newStreamHeader = pipeline.StreamId << 3;
                var newStreamNameBytes = new byte[0];
                await SemaphoreWriteDownstreamMessageAsync(newStreamHeader, newStreamNameBytes, cancellationToken);
            }

            // Start upstream connection reader
            var connectionReaderTask = ExecuteUpstreamConnectionReaderAsync(pipeline);
            _upstreamReaderTasks[(pipeline.Direction, pipeline.StreamId)] = connectionReaderTask;
        }

        private ValueTask<FlushResult> WriteUpstreamMessageAsync(MessageFlag messageFlag, int streamId,
            ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var key = (messageFlag == MessageFlag.MessageInitiator ? Direction.Inbound : Direction.Outbound, streamId);
            if (!_upstreamConnections.TryGetValue(key, out var connection))
            {
                if (Mplex67.s_diagnosticSource.IsEnabled(Mplex67.Diagnostics.UnknownStream))
                {
                    Mplex67.s_diagnosticSource.Write(Mplex67.Diagnostics.UnknownStream, new {Key = key});
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
    }
}
