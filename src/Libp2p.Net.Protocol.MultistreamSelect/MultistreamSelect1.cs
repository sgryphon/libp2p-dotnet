using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Libp2p.Net.Protocol
{
    public class MultistreamSelect1 : Dictionary<string, IProtocol>, IProtocolSelect
    {
        private Task? _readPipeTask;
        private CancellationToken? _readPipeTaskCancellationToken;
        private const string Identifier = "/multistream/1.0.0";
        private const string Na = "na";
        private static readonly byte[] s_identifierBytes;
        private static readonly byte[] s_naBytes;

        private readonly DiagnosticSource s_diagnosticSource =
            new DiagnosticListener("Libp2p.Net.Protocol.MultistreamSelect1");
        
        static MultistreamSelect1()
        {
            // Could set the bytes directly, but the string is more maintainable
            s_identifierBytes = GetLengthPrefixedNewlineTerminatedBytes(Identifier);
            s_naBytes = GetLengthPrefixedNewlineTerminatedBytes(Na);
        }

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            _readPipeTaskCancellationToken = new CancellationToken();
            _readPipeTask = Task.Run(
                () => ReadPipeAsync(connection, _readPipeTaskCancellationToken.Value),
                cancellationToken);
            return Task.CompletedTask;
        }

        public string Name { get { return Identifier; } }

        private static byte[] GetLengthPrefixedNewlineTerminatedBytes(string s)
        {
            var length = Encoding.UTF8.GetByteCount(s) + 1;
            if (length >= 128)
                throw new NotSupportedException();
            
            var bytes = new byte[length + 1];
            bytes[0] = (byte)length;
            bytes[length] = (byte)'\n';
            Encoding.UTF8.GetBytes(s, bytes.AsSpan(1, length - 1));
            
            return bytes;
        }

        private IProtocol? ProcessLine(ReadOnlySequence<byte> buffer)
        {
            var lengthValue = Int32Value.Parser.ParseFrom(buffer);
            var stringValue =
                StringValue.Parser.ParseFrom(buffer.Slice(lengthValue.CalculateSize(), lengthValue.Value - 1));
            if (TryGetValue(stringValue.Value, out var protocol))
            {
                return protocol;
            }

            return null;
        }

        private async Task ReadPipeAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var activity = default(Activity);
            try
            {
                if (s_diagnosticSource.IsEnabled(Diagnostics.ReadPipeActivity))
                {
                    activity = new Activity(Diagnostics.ReadPipeActivity);
                    activity = s_diagnosticSource.StartActivity(activity, null);
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

                // Look for protocol name
                var endLinePosition = (SequencePosition?)null;
                while (true)
                {
                    var result = await connection.Input.ReadAsync(cancellationToken).ConfigureAwait(false);
                    var buffer = result.Buffer;
                    // TODO: Length prefix could contain 0x0A.
                    endLinePosition = buffer.PositionOf((byte)'\n');
                    if (endLinePosition != null)
                    {
                        var protocol = ProcessLine(buffer.Slice(0, endLinePosition.Value));
                        //advance
                        if (protocol != null)
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ProtocolSelected))
                                s_diagnosticSource.Write(Diagnostics.ProtocolSelected, new {Protocol = protocol.Name});
                            //reply buffer
                            //start protocol
                        }
                        else
                        {
                            if (s_diagnosticSource.IsEnabled(Diagnostics.ReplyNa))
                                s_diagnosticSource.Write(Diagnostics.ReplyNa, null);
                            SendNa(connection);
                            await connection.Output.FlushAsync(cancellationToken);
                            await connection.Output.CompleteAsync();
                            await connection.Input.CompleteAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (s_diagnosticSource.IsEnabled(Diagnostics.Exception))
                    s_diagnosticSource.Write(Diagnostics.Exception, new {Exception = ex});
            }
            finally
            {
                if (activity != null)
                {
                    s_diagnosticSource.StopActivity(activity, null);
                }
            }
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
                if (!match) break;
                
                checkedPosition = buffer.GetPosition(bytesToCheck, checkedPosition.Value);
                checkedBytes += bytesToCheck;
                if (lastCheck) break;
            }

            return match;
        }
        
        private static class Diagnostics
        {
            public const string Exception = "MultistreamSelect.Exception";
            public const string ReadPipeActivity = "MultistreamSelect.ReadPipe";
            public const string ProtocolSelected = "MultistreamSelect.ProtocolSelected";
            public const string ReplyNa = "MultistreamSelect.ReplyNa";
        }
    }
}
