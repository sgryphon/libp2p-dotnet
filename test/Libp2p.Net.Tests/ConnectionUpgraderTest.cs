using System;
using System.Data;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Cryptography;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Multiformats.Net;
using Shouldly;

namespace Libp2p.Net.Tests
{
    [TestClass]
    public class ConnectionUpgraderTest
    {
        [TestMethod]
        public async Task UpgradeCreatesConnection()
        {
            using var diagnostics = new TestDiagnosticCollector();

            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new TestMultiplexProtocol("/test/multiplex")};
            var selectors = new IProtocolSelect[] {new TestProtocolSelect()
            {
                [typeof(IEncryptionProtocol)] = encryptors[0],
                [typeof(IMultiplexProtocol)] = multiplexers[0],
            }};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Outbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var upgraded = await upgrader.UpgradeAsync(pipeConnection, cancellation.Token);

            // Assert
            upgraded.Direction.ShouldBe(Direction.Outbound);
            upgraded.LocalAddress?.ToString().ShouldBe("/memory/test1");
            upgraded.RemoteAddress.ToString().ShouldBe("/memory/test2");
            upgraded.EncryptionProtocol?.Identifier.ShouldBe("/plaintext/1.0.0");
            upgraded.MultiplexProtocol?.Identifier.ShouldBe("/test/multiplex");
        }

        [TestMethod]
        public async Task UpgradedCanConnectAndSendInBothDirections()
        {
            using var diagnostics = new TestDiagnosticCollector();

            // NOTE: This is really testing Connection, not the upgrader, but there is no public constructor so we use upgrader to create
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new TestMultiplexProtocol("/test/multiplex")};
            var testProtocol = new TestProtocol("/test/protocol");
            // - note that TestProtocolSelect doesn't send anything (no bytes), it just returns the specified value directly
            var selectors = new IProtocolSelect[] {new TestProtocolSelect()
            {
                [typeof(IEncryptionProtocol)] = encryptors[0],
                [typeof(IMultiplexProtocol)] = multiplexers[0],
                [typeof(IProtocol)] = testProtocol,
            }};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Outbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var upgraded = await upgrader.UpgradeAsync(pipeConnection, cancellation.Token);
            var (pipeline, protocolIdentifier) = await upgraded.ConnectAsync("test/protocol", cancellation.Token);

            // - receive
            _ = await inputPipe.Writer.WriteAsync(new byte[] {0x1, 0x2, 0x3}, cancellation.Token);
            // - send
            _ = await pipeline!.Output.WriteAsync(new byte[] {0x4, 0x5, 0x6}, cancellation.Token);

            // Assert
            var receivedBytes = await PipeUtility.ReadBytesTimeoutAsync(pipeline.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var sendBytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            receivedBytes[0].ShouldBe((byte)0x1);
            sendBytes[0].ShouldBe((byte)0x4);
            protocolIdentifier.ShouldBe("/test/protocol");
        }
        
        [TestMethod]
        public async Task UpgradedCanAcceptAndSendInBothDirections()
        {
            using var diagnostics = new TestDiagnosticCollector();

            // NOTE: This is really testing Connection, not the upgrader, but there is no public constructor so we use upgrader to create
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new TestMultiplexProtocol("/test/multiplex")};
            var testProtocol = new TestProtocol("/test/protocol");
            // - note that TestProtocolSelect doesn't send anything (no bytes), it just returns the specified value directly
            var selectors = new IProtocolSelect[] {new TestProtocolSelect()
            {
                [typeof(IEncryptionProtocol)] = encryptors[0],
                [typeof(IMultiplexProtocol)] = multiplexers[0],
                [typeof(IProtocol)] = testProtocol,
            }};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Outbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var upgraded = await upgrader.UpgradeAsync(pipeConnection, cancellation.Token);
            var (pipeline, protocolIdentifier) = await upgraded.AcceptAsync(new[] {"test/protocol"}, cancellation.Token);

            // - receive
            _ = await inputPipe.Writer.WriteAsync(new byte[] {0x1, 0x2, 0x3}, cancellation.Token);
            // - send
            _ = await pipeline!.Output.WriteAsync(new byte[] {0x4, 0x5, 0x6}, cancellation.Token);

            // Assert
            var receivedBytes = await PipeUtility.ReadBytesTimeoutAsync(pipeline.Input, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            var sendBytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 3,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            receivedBytes[0].ShouldBe((byte)0x1);
            sendBytes[0].ShouldBe((byte)0x4);
            protocolIdentifier.ShouldBe("/test/protocol");
        }
    }
}
