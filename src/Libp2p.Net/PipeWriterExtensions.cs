using System;
using System.IO.Pipelines;
using System.Runtime.Serialization;
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
            VarIntUtility.TryWriteVarInt(outputBuffer, value, out var bytesWritten);
            pipeWriter.Advance(bytesWritten);
            return pipeWriter.FlushAsync(cancellationToken);
        }
    }
}
