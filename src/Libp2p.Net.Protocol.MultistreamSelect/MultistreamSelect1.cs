using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication;

namespace Libp2p.Net.Protocol
{
    public class MultistreamSelect1 : Dictionary<string, IProtocol>, IProtocolSelect
    {
        private Task? _readPipeTask;
        private CancellationToken? _readPipeTaskCancellationToken;
        private const string Identifier = "/multistream/1.0.0";
        private const string Na = "na";

        private readonly DiagnosticSource s_diagnosticSource =
            new DiagnosticListener("Libp2p.Net.Protocol.MultistreamSelect1");

        private static readonly byte[] s_identifierBytes;
        private static readonly byte[] s_naBytes;

        static MultistreamSelect1()
        {
            // Could set the bytes directly, but the string is more maintainable
            s_identifierBytes = GetLengthPrefixedNewlineTerminatedBytes(Identifier);
            s_naBytes = GetLengthPrefixedNewlineTerminatedBytes(Na);
        }

        public string Name => Identifier;

        public static ISystemClock SystemClock { get; set; } = new SystemClock();

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            _readPipeTaskCancellationToken = new CancellationToken();
            _readPipeTask = Task.Run(
                () => ReadPipeAsync(connection, _readPipeTaskCancellationToken.Value),
                cancellationToken);
            return Task.CompletedTask;
        }

        private static class Diagnostics
        {
            public const string Exception = "MultistreamSelect.Exception";
            public const string ProtocolSelected = "MultistreamSelect.ProtocolSelected";
            public const string ReadPipeActivity = "MultistreamSelect.ReadPipe";
            public const string ReplyNa = "MultistreamSelect.ReplyNa";
        }

        private static byte[] GetLengthPrefixedNewlineTerminatedBytes(string s)
        {
            // From the examples, this appears to simply be length prefixed with a protobuf varint;
            // not an actual protobuf field.
            // e.g. "na" is encoded (in the examples) as 0x03 0x6e 0x61 0x0a, with '\n' at the end, and length 3.
            // (A protobuf field would also have the tag, in this case tag = 0x1, left shift 3 to 0x8, plus type 0x2
            // for varint-length-prefixed, i.e. 0x0a 0x03 0x6e 0x61 0x0a.)
            var length = Encoding.UTF8.GetByteCount(s) + 1;
            if (length >= 128)
            {
                throw new NotSupportedException();
            }

            var bytes = new byte[length + 1];
            bytes[0] = (byte)length;
            bytes[length] = (byte)'\n';
            Encoding.UTF8.GetBytes(s, bytes.AsSpan(1, length - 1));

            return bytes;
        }

        private async Task ReadPipeAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var activity = default(Activity);
            try
            {
                if (s_diagnosticSource.IsEnabled(Diagnostics.ReadPipeActivity))
                {
                    activity = new Activity(Diagnostics.ReadPipeActivity);
                    activity = s_diagnosticSource.StartActivity(activity, activity);
                }

                // Match header
                var matchedPosition = (SequencePosition?)null;
                var matchedBytes = 0;
                while (true)
                {
                    var result = await connection.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (!TryMatchPosition(buffer, ref matchedPosition, ref matchedBytes))
                    {
                        // Header not matched (different protocol)
                        // TODO: try different protocol
                        return;
                    }

                    if (matchedBytes == s_identifierBytes.Length)
                    {
                        // Header fully matched
                        connection.Input.AdvanceTo(matchedPosition!.Value, matchedPosition.Value);
                        break;
                    }

                    connection.Input.AdvanceTo(buffer.Start, matchedPosition!.Value);
                }

                // Respond
                SendIdentifier(connection);
                await connection.Output.FlushAsync(cancellationToken);

                // Find length
                var protocolLength = 0;
                while (true)
                {
                    var result = await connection.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (TryReadVarInt(ref buffer, out var consumed, out var length))
                    {
                        var endLengthPosition = buffer.GetPosition(consumed, buffer.Start);
                        connection.Input.AdvanceTo(endLengthPosition, endLengthPosition);
                        protocolLength = length;
                        break;
                    }
                    connection.Input.AdvanceTo(buffer.Start, buffer.End);
                }

                // Read protocol
                while (true)
                {
                    var result = await connection.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (buffer.Length >= protocolLength)
                    {
                        // TODO: Should just try and directly match the bytes
                        var protocolName = Encoding.UTF8.GetString(
                            buffer.Slice(0, protocolLength).ToArray());
                        if (TryGetValue(protocolName, out var protocol))
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ProtocolSelected))
                            {
                                s_diagnosticSource.Write(Diagnostics.ProtocolSelected, new {Protocol = protocol.Name});
                            }

                            //reply buffer
                            //start protocol
                        }
                        else
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ReplyNa))
                            {
                                s_diagnosticSource.Write(Diagnostics.ReplyNa, null);
                            }

                            SendNa(connection);
                            await connection.Output.FlushAsync(cancellationToken);
                            await connection.Output.CompleteAsync();
                            await connection.Input.CompleteAsync();
                            break;
                        }
                    }
                    connection.Input.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (Exception ex)
            {
                if (s_diagnosticSource.IsEnabled(Diagnostics.Exception))
                {
                    s_diagnosticSource.Write(Diagnostics.Exception, ex);
                }
            }
            finally
            {
                if (activity != null)
                {
                    s_diagnosticSource.StopActivity(activity, activity);
                }
            }
        }

        private bool TryReadVarInt(ref ReadOnlySequence<byte> buffer, out long consumed, out int value)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            
            value = 0;
            while (sequenceReader.TryRead(out var b))
            {
                if (sequenceReader.Consumed == 5)
                {
                    if (b > 0x7)
                    {
                        throw new OverflowException("Value to large for Int32");
                    }
                }
                
                if (b < 0x80)
                {
                    value = (value << 7) | b;
                    consumed = sequenceReader.Consumed;
                    return true;
                }
                value = (value << 7) | (b & 0x7F);
            }

            consumed = 0;
            return false;
        }

        private void SendIdentifier(IConnection connection)
        {
            var outputBuffer = connection.Output.GetSpan(s_identifierBytes.Length);
            s_identifierBytes.CopyTo(outputBuffer);
            connection.Output.Advance(s_identifierBytes.Length);
        }

        private void SendNa(IConnection connection)
        {
            var outputBuffer = connection.Output.GetSpan(s_naBytes.Length);
            s_naBytes.CopyTo(outputBuffer);
            connection.Output.Advance(s_naBytes.Length);
        }

        private bool TryMatchPosition(ReadOnlySequence<byte> buffer, ref SequencePosition? checkedPosition,
            ref int checkedBytes)
        {
            checkedPosition ??= buffer.Start;
            var remainingBuffer = buffer.Slice(checkedPosition.Value);
            var remainingBytesToMatch = s_identifierBytes.AsSpan().Slice(checkedBytes);
            var match = true;
            foreach (var segment in remainingBuffer)
            {
                var lastCheck = segment.Length >= remainingBytesToMatch.Length;
                var bytesToCheck = lastCheck ? remainingBytesToMatch.Length : segment.Length;
                match = segment.Slice(0, bytesToCheck).Span.SequenceEqual(remainingBytesToMatch.Slice(0, bytesToCheck));
                if (!match)
                {
                    break;
                }

                checkedPosition = buffer.GetPosition(bytesToCheck, checkedPosition.Value);
                checkedBytes += bytesToCheck;
                if (lastCheck)
                {
                    break;
                }
            }

            return match;
        }
    }
}
