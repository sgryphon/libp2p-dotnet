using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Libp2p.Net.Transport;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Libp2p.Net.Protocol.Tests
{
    [TestClass]
    public class MultistreamSelect1_Listener
    {
        private List<IDisposable> _allListeners = new List<IDisposable>();
        
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
        }
        
        [TestMethod]
        public async Task HandshakeReply()
        {
            // Arrange
            var protocolSelect = new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection);

            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));

            // Assert
            var result = await outputPipe.Reader.ReadAsync();
            var bytes = result.Buffer.ToArray();
            bytes[0].ShouldBe((byte)19);
            bytes.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
        }
        
        [TestMethod]
        public async Task SelectSingleProtocol()
        {
            // Arrange
            var testProtocol1 = new TestProtocol();
            var protocolSelect = (IProtocolSelect)new MultistreamSelect1();
            protocolSelect.Add("/proto/test/1", testProtocol1);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection);
            
            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
            var _ = await outputPipe.Reader.ReadAsync();
            
            await inputPipe.Writer.WriteAsync(new [] {(byte)14});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            
            // Assert
            var result2 = await outputPipe.Reader.ReadAsync();
            var bytes = result2.Buffer.ToArray();
            bytes[0].ShouldBe((byte)14);
            bytes.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            testProtocol1.Connections.Count.ShouldBe(1);
        }
        
        [TestMethod]
        public async Task RespondNa()
        {
            // Arrange
            var testProtocol1 = new TestProtocol();
            var protocolSelect = new MultistreamSelect1();
            protocolSelect.Add("/proto/test/1", testProtocol1);
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var outputStream = outputPipe.Reader.AsStream();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection);
            
            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
            var result1 = await outputPipe.Reader.ReadAsync();
            var bytes1 = result1.Buffer.ToArray();
            outputPipe.Reader.AdvanceTo(result1.Buffer.End, result1.Buffer.End);
            
            await inputPipe.Writer.WriteAsync(new [] {(byte)15});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/nothing\n"));
            
            // Assert
            var result2 = await outputPipe.Reader.ReadAsync();
            var bytes2 = result2.Buffer.ToArray();
            bytes2[0].ShouldBe((byte)3);
            bytes2.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("na\n"));
            testProtocol1.Connections.Count.ShouldBe(0);
        }
        
        [TestMethod]
        public async Task SelectProtocolFromList()
        {
            // Arrange
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
            var outputStream = outputPipe.Reader.AsStream();
            var pipeConnection = new PipeConnection(inputPipe.Reader, outputPipe.Writer);
            await protocolSelect.StartAsync(pipeConnection);

            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
            for (var i = 0; i < 19; i++)
            {
                var b = outputStream.ReadByte();
            }

            await inputPipe.Writer.WriteAsync(new [] {(byte)13});
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/other\n"));
            
            // Assert
            var result2 = await outputPipe.Reader.ReadAsync();
            var bytes = result2.Buffer.ToArray();
            bytes[0].ShouldBe((byte)13);
            bytes.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/other\n"));
            testProtocol1.Connections.Count.ShouldBe(0);
            testProtocolOther.Connections.Count.ShouldBe(1);
        }
    }
}
