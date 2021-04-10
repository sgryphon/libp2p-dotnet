using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Libp2p.Net.Streams.Tests
{
    [TestClass]
    public class Mplex67ConnectTest
    {
        [TestMethod]
        public async Task InitialConnectSendsHeaderPacket()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            var multiplexConnection = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            // Act
            var connection = await multiplexConnection.ConnectAsync(cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 2,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)0x8); // stream ID 1
            bytes[1].ShouldBe((byte)0x0); // stream name empty
            ((MplexConnection)connection).StreamId.ShouldBe(1);
        }
    }
}
