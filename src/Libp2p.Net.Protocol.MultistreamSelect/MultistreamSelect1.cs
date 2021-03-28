using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Libp2p.Net.Protocol
{
    public class MultistreamSelect1 : IProtocolSelect
    {
        private const string identifier = "/multistream/1.0.0";
        private static byte[] headerBytes;

        static MultistreamSelect1()
        {
            // Could set the bytes directly, but the string is more maintainable
            var header = new StringValue() {Value = identifier + "\n"};
            var length = new Int32Value() {Value = header.CalculateSize()};
            var lengthSize = length.CalculateSize();
            headerBytes = new byte[lengthSize + length.Value];
            length.WriteTo(headerBytes.AsSpan(0, lengthSize));
            header.WriteTo(headerBytes.AsSpan(lengthSize));
        }

        public void Start(IConnection connection)
        {
            throw new NotImplementedException();
        }

        public void Add(string identifier, IProtocol protocol)
        {
            throw new NotImplementedException();
        }
    }
}
