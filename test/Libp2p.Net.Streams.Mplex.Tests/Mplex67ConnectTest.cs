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
            // TODO: Wire up Mplex to underlying connection

            // Act
            // TODO: Create new connection
            // protocolMplex.ConnectAsync(...) / CreateConnectionAsync()
            // TODO: Do we need to also send something in the connection?
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            // TODO: Check correct packet header was sent
            //bytes[0].ShouldBe((byte)19);
        }
    }
}
