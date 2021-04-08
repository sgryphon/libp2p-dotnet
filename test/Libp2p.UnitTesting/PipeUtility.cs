using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.UnitTesting
{
    public static class PipeUtility
    {
        public static async Task<byte[]> ReadBytesTimeoutAsync(PipeReader pipeReader, int count, TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var bytes = new byte[0];
            var position = default(SequencePosition);

            try
            {
                var timeoutCts = new CancellationTokenSource(timeout);
                var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                while (true)
                {
                    var result = await pipeReader.ReadAsync(combinedCts.Token); 
                    // This is in no way efficient, but rather than fail outright, we want to keep track of and
                    // return the last thing we did receive for debugging purposes, e.g. maybe some of it matches.
                    bytes = result.Buffer.ToArray();
                    position = result.Buffer.End;

                    if (result.Buffer.Length >= count)
                    {
                        break;
                    }

                    pipeReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"ReadBytesTimeoutAsync timed out with {bytes.Length}/{count} bytes.");
            }

            if (bytes.Length > 0)
            {
                pipeReader.AdvanceTo(position, position);
            }

            return bytes;
        }
    }
}
