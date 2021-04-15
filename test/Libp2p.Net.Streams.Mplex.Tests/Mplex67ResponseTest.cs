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
    public class Mplex67ResponseTest
    {
        [TestMethod]
        public async Task UpstreamMessageShouldBeForwardedToConnection()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(MultiAddress.Parse("/memory/test"), inputPipe.Reader, outputPipe.Writer);
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
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task UpstreamMessagesShouldMultiplexBetweenTwoConnections()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(MultiAddress.Parse("/memory/test"), inputPipe.Reader, outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);
            var connection1 = await multiplexer.ConnectAsync(cancellation.Token);
            var connection2 = await multiplexer.ConnectAsync(cancellation.Token);

            // Act
            var input = new byte[]
            {
                0x12, // stream ID 2 + MessageReceiver (2) 
                4, // length
                0x91, 0x92, 0x93, 0x94
            }.Concat(new byte[]
            {
                0x0a, // stream ID 1 + MessageReceiver (2) 
                3, // length
                0x81, 0x82, 0x83
            }).ToArray();
            await inputPipe.Writer.WriteAsync(input, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(connection1.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {0x81, 0x82, 0x83};
            bytes.ShouldBe(expected);
            
            var bytes2 = await PipeUtility.ReadBytesTimeoutAsync(connection2.Input, 4,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected2 = new byte[] {0x91, 0x92, 0x93, 0x94};
            bytes2.ShouldBe(expected2);
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }

        [TestMethod]
        public async Task NotImplementedYetShouldThrow()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolMplex = new Mplex67();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection =
                new PipeConnection(MultiAddress.Parse("/memory/test"), inputPipe.Reader, outputPipe.Writer);
            var multiplexer = await protocolMplex.StartMultiplexerAsync(pipeConnection, cancellation.Token);
            var connection1 = await multiplexer.ConnectAsync(cancellation.Token);

            // Act
            var input = new byte[]
            {
                0x0d, // stream ID 1 + ResetReceiver (5) 
                0 // length
            };

            var token = cancellation.Token;
            // TODO: The writing should not throw, but it should reject the message and then recover (or close).
            //await Should.ThrowAsync<NotImplementedException>(async () =>
            //{
                await inputPipe.Writer.WriteAsync(input, token);
            //});
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(connection1.Input, 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var expected = new byte[] {};
            bytes.ShouldBe(expected);
            
            diagnostics.GetExceptions().Count.ShouldBe(1);
        }
    }
}
