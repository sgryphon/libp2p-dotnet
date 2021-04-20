using System;
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

    }
}
