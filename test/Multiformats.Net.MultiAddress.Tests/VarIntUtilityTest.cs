using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Multiformats.Net.Tests
{
    [TestClass]
    public class VarIntUtilityTest
    {
        [TestMethod]
        [DataRow(0, new byte[] {0x0})]
        [DataRow(1, new byte[] {0x1})]
        [DataRow(0x79, new byte[] {0x79})]
        [DataRow(0x80, new byte[] {0x80, 0x01})]
        [DataRow(0xFF, new byte[] {0xFF, 0x01})]
        [DataRow(int.MaxValue, new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0x07})]
        [DataRow(-1, new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0x0F})]
        [DataRow(int.MinValue, new byte[] {0x80, 0x80, 0x80, 0x80, 0x08})]
        public void WriteCorrectInt32Bytes(int value, byte[] expectedBytes)
        {
            var buffer = new Span<byte>(new byte[5]);
            
            VarIntUtility.WriteVarInt(buffer, value, out var bytesWritten);
            
            bytesWritten.ShouldBe(expectedBytes.Length);
            buffer.Slice(0, bytesWritten).ToArray().ShouldBe(expectedBytes);
        }
        
        [TestMethod]
        [DataRow(0uL, new byte[] {0x0})]
        [DataRow(1uL, new byte[] {0x1})]
        [DataRow(0x07_FFFFFFFFuL, new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0x7F})]
        [DataRow(0x0F_FFFFFFFFuL, new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01})]
        [DataRow(ulong.MaxValue, new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01})]
        public void WriteCorrectUInt64Bytes(ulong value, byte[] expectedBytes)
        {
            var buffer = new Span<byte>(new byte[10]);
            
            var success = VarIntUtility.TryWriteVarInt(buffer, value, out var bytesWritten);
            
            success.ShouldBeTrue();
            bytesWritten.ShouldBe(expectedBytes.Length);
            buffer.Slice(0, bytesWritten).ToArray().ShouldBe(expectedBytes);
        }
        
        [TestMethod]
        public void ShouldFailInValueDoesNotFitInBuffer()
        {
            var buffer = new Span<byte>(new byte[5]);
            var value = 0x0F_FFFFFFFFuL;

            var success = VarIntUtility.TryWriteVarInt(buffer, value, out var bytesWritten);
            
            success.ShouldBeFalse();
        }
    }
}
