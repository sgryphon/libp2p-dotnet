using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net.Transport;
using Libp2p.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Libp2p.Net.Protocol.Tests
{
    [TestClass]
    public class MultistreamSelect1_Listener
    {
        private readonly List<IDisposable> _allListeners = new();
        
        [TestInitialize]
        public void Initialize()
        {
            _allListeners.Add(DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                if (listener.Name.StartsWith("Libp2p."))
                {
                    _allListeners.Add(listener.Subscribe(kvp =>
                    {
                        Console.WriteLine("{0}: {1}", kvp.Key, kvp.Value);
                    }));
                }
            }));
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var listener in _allListeners)
            {
                listener.Dispose();
            }
            _allListeners.Clear();
        }
        
        [TestMethod]
        public async Task HandshakeReply()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var protocolSelect = new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection, cancellation.Token);

            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
        }
        
        [TestMethod]
        public async Task SelectSingleProtocol()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol();
            var protocolSelect = (IProtocolSelect)new MultistreamSelect1();
            protocolSelect.Add("/proto/test/1", testProtocol1);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection, cancellation.Token);
            
            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)14}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/test/1\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 14, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)14);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            testProtocol1.Connections.Count.ShouldBe(1);
        }
        
        [TestMethod]
        public async Task SelectSingleProtocolWithDelays()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol();
            var protocolSelect = (IProtocolSelect)new MultistreamSelect1();
            protocolSelect.Add("/proto/test/1", testProtocol1);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection, cancellation.Token);
            
            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistrea"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("m/1.0.0\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)14}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/t"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("est/1\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 14, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)14);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            testProtocol1.Connections.Count.ShouldBe(1);
        }
        
        [TestMethod]
        public async Task RespondNa()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol();
            var protocolSelect = new MultistreamSelect1();
            protocolSelect.Add("/proto/test/1", testProtocol1);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection, cancellation.Token);
            
            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)15}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/nothing\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 3, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)3);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("na\n"));
            testProtocol1.Connections.Count.ShouldBe(0);
        }

        [TestMethod]
        public async Task SelectProtocolFromList()
        {
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol();
            var testProtocolOther = new TestProtocol();
            var protocolSelect = new MultistreamSelect1()
            {
                ["/proto/test/1"] = testProtocol1,
                ["/proto/other"] = testProtocolOther,
                ["/proto/test/2"] = testProtocol1,
            };
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection, cancellation.Token);

            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)13}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/other\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 13, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)13);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/other\n"));
            testProtocol1.Connections.Count.ShouldBe(0);
            testProtocolOther.Connections.Count.ShouldBe(1);
        }
    }
}
