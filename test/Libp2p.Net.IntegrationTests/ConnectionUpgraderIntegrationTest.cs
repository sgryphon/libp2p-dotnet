using System;
using System.Data;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Cryptography;
using Libp2p.Net.Protocol;
using Libp2p.Net.Streams;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Multiformats.Net;
using Shouldly;
using Shouldly.Configuration;

namespace Libp2p.Net.IntegrationTests
{
    [TestClass]
    public class ConnectionUpgraderIntegrationTest
    {
        [TestMethod]
        public async Task UpgradeCreatesInboundConnection()
        {
            using var diagnostics = new TestDiagnosticCollector();

            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var selectors = new IProtocolSelect[] {new MultistreamSelect1()};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var upgradeTask = upgrader.UpgradeAsync(pipeConnection, cancellation.Token);

            var incoming1Bytes = Encoding.UTF8.GetBytes("/multistream/1.0.0\n");
            var incoming2Bytes = Encoding.UTF8.GetBytes("/plaintext/1.0.0\n");
            var incoming4Bytes = Encoding.UTF8.GetBytes("/mplex/6.7.0\n");
            
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)incoming1Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(incoming1Bytes, cancellation.Token);
            var response1Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, incoming1Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)incoming2Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(incoming2Bytes, cancellation.Token);
            var response2Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, incoming2Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)incoming1Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(incoming1Bytes, cancellation.Token);
            var response3Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, incoming1Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);

            await inputPipe.Writer.WriteAsync(new byte[] {(byte)incoming4Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(incoming4Bytes, cancellation.Token);
            var response4Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, incoming4Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);

            // Assert
            var upgraded = upgradeTask.Result;
            upgraded.Direction.ShouldBe(Direction.Outbound);
            upgraded.EncryptionProtocol?.Identifier.ShouldBe("/plaintext/1.0.0");
            upgraded.MultiplexProtocol?.Identifier.ShouldBe("/mplex/6.7.0");
        }
        
                [TestMethod]
        public async Task UpgradeCreatesOutboundConnection()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var selectors = new IProtocolSelect[] {new MultistreamSelect1()};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Outbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var upgradeTask = upgrader.UpgradeAsync(pipeConnection, cancellation.Token);

            var response1Bytes = Encoding.UTF8.GetBytes("/multistream/1.0.0\n");
            var response2Bytes = Encoding.UTF8.GetBytes("/plaintext/1.0.0\n");
            var response4Bytes = Encoding.UTF8.GetBytes("/mplex/6.7.0\n");
            
            var received1Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, response1Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)response1Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(response1Bytes, cancellation.Token);
            
            var received2Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, response2Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)response2Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(response2Bytes, cancellation.Token);
            
            var received3Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, response1Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)response1Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(response1Bytes, cancellation.Token);

            var received4Bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, response4Bytes.Length + 1,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new byte[] {(byte)response4Bytes.Length}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(response4Bytes, cancellation.Token);

            // Assert
            var upgraded = upgradeTask.Result;
            upgraded.Direction.ShouldBe(Direction.Outbound);
            upgraded.EncryptionProtocol?.Identifier.ShouldBe("/plaintext/1.0.0");
            upgraded.MultiplexProtocol?.Identifier.ShouldBe("/mplex/6.7.0");
        }
    }
}
