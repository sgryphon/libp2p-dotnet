using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Libp2p.Net.Protocol
{
    // See: https://github.com/multiformats/multistream-select
    public class MultistreamSelect1<T> : Dictionary<string, T>, IProtocolSelect<T> where T: class
    {
        private const string Identifier = "/multistream/1.0.0";
        private const string Na = "na";

        private static readonly DiagnosticSource s_diagnosticSource =
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

        public async Task<T?> SelectProtocolAsync(IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            try
            {
                var protocol = await NegotiateProtocol(pipeline, cancellationToken);
                return protocol;
            }
            catch (Exception ex)
            {
                if (s_diagnosticSource.IsEnabled(Diagnostics.Exception))
                {
                    s_diagnosticSource.Write(Diagnostics.Exception, ex);
                }

                return null;
            }
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

        private async Task<T?> NegotiateProtocol(IPipeline pipeline, CancellationToken cancellationToken)
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
                    var result = await pipeline.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (!TryMatchPosition(buffer, ref matchedPosition, ref matchedBytes))
                    {
                        // Header not matched (different protocol), i.e. try different protocol selection
                        return null;
                    }

                    if (matchedBytes == s_identifierBytes.Length)
                    {
                        // Header fully matched
                        pipeline.Input.AdvanceTo(matchedPosition!.Value, matchedPosition.Value);
                        break;
                    }

                    pipeline.Input.AdvanceTo(buffer.Start, matchedPosition!.Value);
                }

                // Respond with header
                var headerFlush = await pipeline.Output.WriteAsync(s_identifierBytes, cancellationToken)
                    .ConfigureAwait(false);

                // Find length
                int protocolLength;
                while (true)
                {
                    var result = await pipeline.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (buffer.TryReadVarInt(out var consumed, out var length))
                    {
                        var endLengthPosition = buffer.GetPosition(consumed, buffer.Start);
                        pipeline.Input.AdvanceTo(endLengthPosition, endLengthPosition);
                        protocolLength = length;
                        break;
                    }

                    pipeline.Input.AdvanceTo(buffer.Start, buffer.End);
                }

                // Read protocol
                while (true)
                {
                    var result = await pipeline.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    if (buffer.Length >= protocolLength)
                    {
                        // TODO: Should just try and directly match the bytes
                        var protocolIdBytes = buffer.Slice(0, protocolLength).ToArray();
                        var protocolId = Encoding.UTF8.GetString(protocolIdBytes);
                        if (TryGetValue(protocolId.TrimEnd('\n'), out var protocol))
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ProtocolSelected))
                            {
                                s_diagnosticSource.Write(Diagnostics.ProtocolSelected,
                                    new {ProtocolId = protocolId});
                            }

                            // Reply protocol name
                            var lengthFlush = await pipeline.Output
                                .WriteVarIntAsync(protocolIdBytes.Length, cancellationToken).ConfigureAwait(false);
                            var bytesFlush = await pipeline.Output.WriteAsync(protocolIdBytes, cancellationToken)
                                .ConfigureAwait(false);
                            return protocol;
                        }
                        else
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ReplyNa))
                            {
                                s_diagnosticSource.Write(Diagnostics.ReplyNa, new {ProtocolId = protocolId});
                            }

                            var naFlush = await pipeline.Output.WriteAsync(s_naBytes, cancellationToken)
                                .ConfigureAwait(false);
                            return null;
                        }
                    }

                    pipeline.Input.AdvanceTo(buffer.Start, buffer.End);
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
