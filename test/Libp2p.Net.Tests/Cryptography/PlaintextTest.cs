using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Cryptography;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Multiformats.Net;
using Shouldly;

namespace Libp2p.Net.Tests.Cryptography
{
    [TestClass]
    public class PlaintextTest
    {
        [TestMethod]
        public async Task DownstreamPipeHasPlaintextValue()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolPlaintext = new Plaintext();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(Direction.Outbound, MultiAddress.Parse("/memory/test"),
                inputPipe.Reader, outputPipe.Writer);
            var encryptedConnection = await protocolPlaintext.StartEncryptionAsync(pipeConnection, cancellation.Token);

            // Act
            _ = await encryptedConnection.Output.WriteAsync(new byte[] {0x1, 0x2, 0x3}, cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)0x1);
            bytes[1].ShouldBe((byte)0x2);
            bytes[2].ShouldBe((byte)0x3);
        }
    }
}
