using System;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
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
    public class Mplex67AcceptTest
    {
        [TestMethod]
        public async Task IncomingNewStreamCreatesNewAccept()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Inbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            var input = new byte[]
            {
                0x08, // stream ID 1 + NewStream (0) 
                0 // length
            };
            var inputFlush = inputPipe.Writer.WriteAsync(input, cancellation.Token);

            // Act
            var connection = await multiplexer.AcceptConnectionAsync(cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            ((MplexConnection)connection).StreamId.ShouldBe(1);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task IncomingStreamAcceptAndReadMessage()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Inbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            var input = new byte[]
            {
                0x08, // stream ID 1 + NewStream (0) 
                0 // length
            };
            var input2 = new byte[]
            {
                0x0a, // stream ID 1 + MessageInitiator (2) 
                3, // length
                0x81, 0x82, 0x83
            };
            var inputFlush = inputPipe.Writer.WriteAsync(input, cancellation.Token);
            var connection = await multiplexer.AcceptConnectionAsync(cancellation.Token);

            // Act
            var inputFlush2 = inputPipe.Writer.WriteAsync(input2, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(connection.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {0x81, 0x82, 0x83};
            bytes.ShouldBe(expected);
            ((MplexConnection)connection).StreamId.ShouldBe(1);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        
        [TestMethod]
        public async Task IncomingStreamAcceptMultiplex()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(Direction.Inbound, MultiAddress.Parse("/memory/test"), inputPipe.Reader,
                    outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);

            var input = new byte[]
            {
                0x08, // stream ID 1 + NewStream (0) 
                0 // length
            }.Concat(new byte[]
            {
                0x10, // stream ID 2 + NewStream (0) 
                0 // length
            }).Concat(new byte[]
            {
                0x12, // stream ID 2 + MessageInitiator (2) 
                4, // length
                0x91, 0x92, 0x93, 0x94
            }).Concat(new byte[]
            {
                0x0a, // stream ID 1 + MessageInitiator (2) 
                3, // length
                0x81, 0x82, 0x83
            }).ToArray();
            var inputFlush = inputPipe.Writer.WriteAsync(input, cancellation.Token);

            // Act
            var connection = await multiplexer.AcceptConnectionAsync(cancellation.Token);
            var connection2 = await multiplexer.AcceptConnectionAsync(cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(connection.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {0x81, 0x82, 0x83};
            bytes.ShouldBe(expected);
            ((MplexConnection)connection).StreamId.ShouldBe(1);

            var bytes2 = await PipeUtility.ReadBytesTimeoutAsync(connection2.Input, 4,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected2 = new byte[] {0x91, 0x92, 0x93, 0x94};
            bytes2.ShouldBe(expected2);
            ((MplexConnection)connection2).StreamId.ShouldBe(2);

            diagnostics.GetExceptions().ShouldBeEmpty();
        }
    }
}
