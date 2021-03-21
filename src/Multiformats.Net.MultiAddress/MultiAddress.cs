using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Multiformats.Net
{
    public class MultiAddress
    {
        private readonly byte[] _bytes;

        private MultiAddress(byte[] bytes)
        {
            _bytes = bytes;
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
                    bytes.Add((byte)Protocol.Ip4);
                    var ipAddress = IPAddress.Parse(parts[index + 1]);
                    //ipAddress.TryWriteBytes()
                    var valueBytes = ipAddress.GetAddressBytes();
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
                else if (name == "ip6")
                {
                    bytes.Add((byte)Protocol.Ip6);
                    var ipAddress = IPAddress.Parse(parts[index + 1]);
                    // TODO: ipAddress.TryWriteBytes()
                    // TODO: Need to check byte order
                    var valueBytes = ipAddress.GetAddressBytes();
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
                else if (name == "tcp")
                {
                    bytes.Add((byte)Protocol.Tcp);
                    var port = short.Parse(parts[index + 1]);
                    var valueBytes = new[] {(byte)(port >> 8), (byte)(port & 0xFF)};
                    bytes.AddRange(valueBytes);
                    index += 2;
                }
            }

            return new MultiAddress(bytes.ToArray());
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var index = 0;
            while (index < _bytes.Length)
            {
                var code = ReadVarInt(index, out var length);
                index += length;
                switch (code)
                {
                    case (int)Protocol.Ip4:
                        builder.Append("/ip4");
                        var ip4Address = new IPAddress(_bytes.AsSpan().Slice(index, 4));
                        builder.AppendFormat("/{0}", ip4Address);
                        index += 4;
                        break;
                    case (int)Protocol.Ip6:
                        builder.Append("/ip6/");
                        var ip6Address = new IPAddress(_bytes.AsSpan().Slice(index, 16));
                        builder.AppendFormat("/{0}", ip6Address);
                        index += 16;
                        break;
                    case (int)Protocol.Tcp:
                        builder.Append("/tcp/");
                        var tcpPort = BitConverter.ToInt16(_bytes.AsSpan().Slice(index, 2));
                        builder.AppendFormat("/{0}", tcpPort);
                        index += 2;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return builder.ToString();
        }

        private int ReadVarInt(int index, out int length)
        {
            var value = _bytes[index];
            if (value > 0x80)
            {
                throw new NotImplementedException();
            }

            length = 1;
            return value;
        }
    }
}
