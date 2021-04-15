using System;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Multiformats.Net;
using Shouldly;

namespace Libp2p.Net.Streams.Tests
{
    [TestClass]
    public class Mplex67ConnectTest
    {
        [TestMethod]
        public async Task InitialConnectSendsHeaderPacket()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Outbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            // Act
            var connection = await multiplexer.ConnectAsync(cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 2,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)0x8); // stream ID 1 + NewStream (0)
            bytes[1].ShouldBe((byte)0x0); // stream name empty
            ((MplexConnection)connection).StreamId.ShouldBe(1);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task MessageSentWithHeader()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Outbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            // Act
            var connection = await multiplexer.ConnectAsync(cancellation.Token);
            var writeFlush = await connection.Output.WriteAsync(new byte[] {0x81, 0x82, 0x83}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 2 + 2 + 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            ((MplexConnection)connection).StreamId.ShouldBe(1);
            ((MplexConnection)connection).Direction.ShouldBe(Direction.Outbound);
            var expected = new byte[] {0x08, 0}
                .Concat(new byte[]
                {
                    0x0a, // stream ID 1 + MessageInitiator (2) 
                    3, // length
                    0x81, 0x82, 0x83
                });
            bytes.ShouldBe(expected);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }

        
        [TestMethod]
        public async Task MultiplexTwoConnections()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Outbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            // Act
            var connection1 = await multiplexer.ConnectAsync(cancellation.Token);
            var connection2 = await multiplexer.ConnectAsync(cancellation.Token);
            var write1Flush = await connection1.Output.WriteAsync(new byte[] {0x81, 0x82, 0x83}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            var write2Flush = await connection2.Output.WriteAsync(new byte[] {0x91, 0x92, 0x93, 0x94}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 2 + 2 + 2 + 3 + 2 + 4,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {0x08, 0}
                .Concat(new byte[] {0x10, 0})
                .Concat(new byte[] {0x0a, 3, 0x81, 0x82, 0x83})
                .Concat(new byte[] {0x12, 4, 0x91, 0x92, 0x93, 0x94});
            bytes.ShouldBe(expected);
            ((MplexConnection)connection2).StreamId.ShouldBe(2);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
    }
}
