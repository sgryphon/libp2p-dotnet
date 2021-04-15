using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Multiformats.Net
{
    public class MultiAddress
    {
        private readonly byte[] _bytes;
        private string? _readable;

        internal MultiAddress(byte[] bytes)
        {
            _bytes = bytes;
        }

        internal MultiAddress(byte[] bytes, string readable)
        {
            _bytes = bytes;
            _readable = readable;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return _bytes;
        }

        public static MultiAddress Create(ReadOnlySpan<byte> bytes)
        {
            var b = new byte[bytes.Length];
            bytes.CopyTo(b);
            return new MultiAddress(b);
        }

        public static MultiAddress Parse(string s)
        {
            // TODO: Implement version for ReadOnlySpan<char>
            // TODO: Make protocols generic handlers for values, etc
            // TODO: Not sure if we need UTF-8 direct conversion? (will always be bytes, except for display and bootstrap values)
            var parts = s.Split('/');
            var index = 0;
            var bytes = new List<byte>();
            while (index < parts.Length)
            {
                var name = parts[index];
                if (index == 0)
                {
                    if (name != "")
                    {
                        throw new ArgumentException();
                    }

                    index++;
                }
                else if (name == "ip4")
                {
                    bytes.Add((byte)ProtocolType.Ip4);
                    var ipAddress = IPAddress.Parse(parts[index + 1]);
                    //ipAddress.TryWriteBytes()
                    var valueBytes = ipAddress.GetAddressBytes();
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
                else if (name == "ip6")
                {
                    bytes.Add((byte)ProtocolType.Ip6);
                    var ipAddress = IPAddress.Parse(parts[index + 1]);
                    // TODO: ipAddress.TryWriteBytes()
                    // TODO: Need to check byte order
                    var valueBytes = ipAddress.GetAddressBytes();
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
                else if (name == "tcp")
                {
                    bytes.Add((byte)ProtocolType.Tcp);
                    var port = short.Parse(parts[index + 1]);
                    var valueBytes = new[] {(byte)(port >> 8), (byte)(port & 0xFF)};
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
                else if (name == "memory")
                {
                    WriteVarInt(bytes, (int)ProtocolType.Memory);
                    var utf8Bytes = Encoding.UTF8.GetBytes(parts[index + 1]);
                    WriteVarInt(bytes, utf8Bytes.Length);
                    bytes.AddRange(utf8Bytes);
                    index += 2;
                }
            }

            return new MultiAddress(bytes.ToArray(), s);
        }

        public override string ToString()
        {
            return _readable ??= GetString(_bytes);
        }

        private static string GetString(byte[] bytes)
        {
            var builder = new StringBuilder();
            var index = 0;
            while (index < bytes.Length)
            {
                VarIntUtility.ReadVarInt(bytes.AsSpan(index), out int protocol, out var protocolBytes);
                index += protocolBytes;
                switch (protocol)
                {
                    case (int)ProtocolType.Ip4:
                        builder.Append("/ip4/");
                        var ip4Address = new IPAddress(bytes.AsSpan().Slice(index, 4));
                        builder.AppendFormat("{0}", ip4Address);
                        index += 4;
                        break;
                    case (int)ProtocolType.Ip6:
                        builder.Append("/ip6/");
                        var ip6Address = new IPAddress(bytes.AsSpan().Slice(index, 16));
                        builder.AppendFormat("{0}", ip6Address);
                        index += 16;
                        break;
                    case (int)ProtocolType.Tcp:
                        builder.Append("/tcp/");
                        var tcpPort = BitConverter.ToInt16(bytes.AsSpan().Slice(index, 2));
                        builder.AppendFormat("{0}", tcpPort);
                        index += 2;
                        break;
                    case (int)ProtocolType.Memory:
                        builder.Append("/memory/");
                        VarIntUtility.ReadVarInt(bytes.AsSpan(index), out int memoryLength, out var memoryLengthBytes);
                        index += memoryLengthBytes;
                        var utf8 = Encoding.UTF8.GetString(bytes.AsSpan(index, memoryLength));
                        builder.AppendFormat("{0}", utf8);
                        index += memoryLength;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return builder.ToString();
        }

        private static void WriteVarInt(List<byte> bytes, int value)
        {
            var buffer = new Span<byte>(new byte[5]);
            VarIntUtility.WriteVarInt(buffer, value, out var bytesWritten);
            bytes.AddRange(buffer[..bytesWritten].ToArray());
        }
    }
}
