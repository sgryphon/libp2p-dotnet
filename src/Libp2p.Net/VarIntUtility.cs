using System;
using System.Runtime.Serialization;

namespace Libp2p.Net
{
    public static class VarIntUtility
    {
        public static void WriteVarInt(Span<byte> buffer, int value, out int bytesWritten)
        {
            if (!TryWriteVarInt(buffer, value, out bytesWritten))
            {
                throw new SerializationException("Failed to serialize varint");
            }
        }
        
        public static bool TryWriteVarInt(Span<byte> buffer, int value, out int bytesWritten)
        {
            var index = 0;
            while (true)
            {
                if (index >= buffer.Length)
                {
                    bytesWritten = 0;
                    return false;
                }
                
                if (value < 0x80)
                {
                    buffer[index] = (byte)value;
                    break;
                }

                buffer[index] = (byte)((value & 0x7F) | 0x80);
                value = value >> 7;
                index++;
            }

            bytesWritten = index + 1;
            return true;
        }
    }
}
