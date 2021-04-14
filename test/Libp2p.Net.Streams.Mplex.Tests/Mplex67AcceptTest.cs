using System;
using System.IO.Pipelines;
using System.Linq;
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
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
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
            ((MplexConnection)connection).StreamId.ShouldBe(1);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
    }
}
