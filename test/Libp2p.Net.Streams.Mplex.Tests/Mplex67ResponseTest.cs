using System;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Libp2p.Net.Streams.Tests
{
    [TestClass]
    public class Mplex67ResponseTest
    {
        [TestMethod]
        public async Task UpstreamMessageShouldBeForwardedToConnection()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);
            var connection1 = await multiplexer.ConnectAsync(cancellation.Token);

            // Act
            var input = new byte[]
            {
                0x0a, // stream ID 1 + MessageReceiver (2) 
                3, // length
                0x81, 0x82, 0x83
            };
            await inputPipe.Writer.WriteAsync(input, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(connection1.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {0x81, 0x82, 0x83};
            bytes.ShouldBe(expected);
        }
    }
}
