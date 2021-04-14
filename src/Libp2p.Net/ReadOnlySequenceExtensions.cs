using System;
using System.Buffers;

namespace Libp2p.Net
{
    public static class ReadOnlySequenceExtensions
    {
        public static bool TryReadVarInt(this ReadOnlySequence<byte> buffer, out long consumed, out int value)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);

            value = 0;
            while (sequenceReader.TryRead(out var b))
            {
                if (sequenceReader.Consumed == 5)
                {
                    if (b > 0x7)
                    {
                        throw new OverflowException("Value too large for Int32");
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
    }
}
