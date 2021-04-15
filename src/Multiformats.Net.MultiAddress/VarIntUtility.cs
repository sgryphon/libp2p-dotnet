using System;
using System.Runtime.Serialization;

namespace Multiformats.Net
{
    public static class VarIntUtility
    {
        public static void ReadVarInt(Span<byte> buffer, out int value, out int bytesRead)
        {
            if (!TryReadVarInt(buffer, out var varInt, out var varIntBytesRead))
            {
                throw new SerializationException("Failed to read varint");
            }

            if (varInt > uint.MaxValue)
            {
                throw new SerializationException("Value too large for Int32");
            }

            bytesRead = varIntBytesRead;
            value = (int)varInt;
        }

        public static void ReadVarInt(Span<byte> buffer, out long value, out int bytesRead)
        {
            if (!TryReadVarInt(buffer, out var varInt, out bytesRead))
            {
                throw new SerializationException("Failed to read varint");
            }

            value = (long)varInt;
        }

        public static void WriteVarInt(Span<byte> buffer, int value, out int bytesWritten)
        {
            if (!TryWriteVarInt(buffer, (uint)value, out bytesWritten))
            {
                throw new SerializationException("Failed to serialize varint");
            }
        }
        
        public static bool TryReadVarInt(ReadOnlySpan<byte> buffer, out ulong value, out int bytesRead)
        {
            value = 0uL;
            var index = 0;
            while (index < buffer.Length)
            {
                var b = buffer[index];
                if (index == 10)
                {
                    if (b > 0x1)
                    {
                        throw new OverflowException("Value too large for UInt64");
                    }
                }

                if (b < 0x80)
                {
                    value = (value << 7) | b;
                    bytesRead = index + 1;
                    return true;
                }

                value = (value << 7) | (b & 0x7FuL);
                index++;
            }

            bytesRead = 0;
            return false;
        }
        
        public static bool TryWriteVarInt(Span<byte> buffer, ulong value, out int bytesWritten)
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
