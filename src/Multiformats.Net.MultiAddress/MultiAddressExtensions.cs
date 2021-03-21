using System;
using System.Net;

namespace Multiformats.Net
{
    public static class MultiAddressExtensions
    {
        public static IPAddress ToIPAddress(this MultiAddress multiAddress)
        {
            var bytes = multiAddress.AsSpan();
            if (bytes[0] == (byte)Protocol.Ip4)
            {
                var ip4Address = new IPAddress(bytes.Slice(1, 4));
                return ip4Address;
            }

            if (bytes[0] == (byte)Protocol.Ip6)
            {
                var ip6Address = new IPAddress(bytes.Slice(1, 16));
                return ip6Address;
            }

            throw new InvalidOperationException();
        }

        public static IPEndPoint ToIPEndPoint(this MultiAddress multiAddress)
        {
            var bytes = multiAddress.AsSpan();
            var ipLength = 0;
            if (bytes[0] == (byte)Protocol.Ip4)
            {
                ipLength = 4;
            }
            else if (bytes[0] == (byte)Protocol.Ip6)
            {
                ipLength = 16;
            }
            else
            {
                throw new InvalidOperationException();
            }

            if (bytes[1 + ipLength] != (byte)Protocol.Tcp)
            {
                throw new InvalidOperationException();
            }

            var ipAddress = new IPAddress(bytes.Slice(1, ipLength));
            var port = (bytes[ipLength + 2] << 8) + bytes[ipLength + 3];
            return new IPEndPoint(ipAddress, port);
        }
    }
}
