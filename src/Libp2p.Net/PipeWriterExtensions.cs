using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net
{
    public static class PipeWriterExtensions
    {
        public static ValueTask<FlushResult> WriteVarIntAsync(this PipeWriter pipeWriter, int value, CancellationToken cancellationToken = default)
        {
            // TODO: Check it handles negatives, etc
            var outputBuffer = pipeWriter.GetSpan(5);
            var index = 0;
            while (true)
            {
                if (value < 0x80)
                {
                    outputBuffer[index] = (byte)value;
                    break;
                }

                outputBuffer[index] = (byte)((value & 0x7F) | 0x80);
                value = value >> 7;
                index++;
            }

            pipeWriter.Advance(index + 1);
            return pipeWriter.FlushAsync(cancellationToken);
        }
    }
}
