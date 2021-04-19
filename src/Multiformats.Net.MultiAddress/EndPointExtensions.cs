using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Multiformats.Net
{
    public static class EndPointExtensions
    {
        public static MultiAddress ToMultiAddress(this EndPoint endPoint)
        {
            if (!(endPoint is IPEndPoint ipEndPoint))
            {
                throw new NotImplementedException();
            }

            var bytes = new List<byte>();
            // Protocol IP
            if (endPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                bytes.Add((byte)ProtocolType.Ip4);
            }
            else if (bytes[0] == (byte)ProtocolType.Ip6)
            {
                bytes.Add((byte)ProtocolType.Ip6);
            }
            else
            {
                throw new InvalidOperationException();
            }

            bytes.AddRange(ipEndPoint.Address.GetAddressBytes());

            // Protocol TCP
            bytes.Add((byte)ProtocolType.Tcp);
            bytes.Add((byte)(ipEndPoint.Port >> 8));
            bytes.Add((byte)(ipEndPoint.Port & 0xFF));

            return new MultiAddress(bytes.ToArray());
        }
    }
}
