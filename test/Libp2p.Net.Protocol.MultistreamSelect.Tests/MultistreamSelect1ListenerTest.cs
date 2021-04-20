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
using Multiformats.Net;
using Shouldly;

namespace Libp2p.Net.Protocol.Tests
{
    [TestClass]
    public class MultistreamSelect1ListenerTest
    {
        [TestMethod]
        public async Task HandshakeReply()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol("/proto/test/1");
            var protocolSelect = new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            var selectTask =
                protocolSelect.ListenProtocolAsync(pipeConnection, new IProtocol[] {testProtocol1}, cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);

            // Assert
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19,
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes.AsSpan(1).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"));
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task SelectSingleProtocol()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol("/proto/test/1");
            var protocolSelect = (IProtocolSelect)new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);
            
            // Act
            var selectTask =
                protocolSelect.ListenProtocolAsync(pipeConnection, new IProtocol[] {testProtocol1}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)14}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/test/1\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var selected = selectTask.Result;
            selected.ShouldBe(testProtocol1);
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 14, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)14);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task SelectSingleProtocolWithDelays()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol("/proto/test/1");
            var protocolSelect = (IProtocolSelect)new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);
            
            // Act
            var selectTask =
                protocolSelect.ListenProtocolAsync(pipeConnection, new IProtocol[] {testProtocol1}, cancellation.Token);
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
            var selected = selectTask.Result;
            selected.ShouldBe(testProtocol1);
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 14, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)14);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/test/1\n"));
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
        
        [TestMethod]
        public async Task RespondNa()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol("/proto/test/1");
            var protocolSelect = new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);
            
            // Act
            var selectTask =
                protocolSelect.ListenProtocolAsync(pipeConnection, new IProtocol[] {testProtocol1}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)15}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/nothing\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var selected = selectTask.Result;
            selected.ShouldBeNull();
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 3, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)3);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("na\n"));
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }

        [TestMethod]
        public async Task SelectProtocolFromList()
        {
            using var diagnostics = new TestDiagnosticCollector();
            
            // Arrange
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            var testProtocol1 = new TestProtocol("/proto/test/1");
            var testProtocolOther = new TestProtocol("/proto/other");
            var testProtocol2 = new TestProtocol("/proto/test/2");
            var protocolSelect = new MultistreamSelect1();
            
            var inputPipe = new Pipe();
            var outputPipe = new Pipe();
            var pipeConnection = new PipeConnection(MultiAddress.Parse("/memory/test1"),
                MultiAddress.Parse("/memory/test2"), Direction.Inbound, inputPipe.Reader, outputPipe.Writer);

            // Act
            var selectTask =
                protocolSelect.ListenProtocolAsync(pipeConnection,
                    new IProtocol[] {testProtocol1, testProtocolOther, testProtocol2}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)19}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/multistream/1.0.0\n"), cancellation.Token);
            await inputPipe.Writer.WriteAsync(new [] {(byte)13}, cancellation.Token);
            await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("/proto/other\n"), cancellation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellation.Token);
            
            // Assert
            var selected = selectTask.Result;
            selected.ShouldBe(testProtocolOther);
            var bytes = await PipeUtility.ReadBytesTimeoutAsync(outputPipe.Reader, 1 + 19 + 1 + 13, 
                TimeSpan.FromMilliseconds(100), cancellation.Token);
            bytes[0].ShouldBe((byte)19);
            bytes[20].ShouldBe((byte)13);
            bytes.AsSpan(21).ToArray().ShouldBe(Encoding.UTF8.GetBytes("/proto/other\n"));
            
            diagnostics.GetExceptions().ShouldBeEmpty();
        }
    }
}
