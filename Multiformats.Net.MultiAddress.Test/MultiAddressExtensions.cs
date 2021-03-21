using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Multiformats.Net.Test
{
    [TestClass]
    public class MultiAddressExtensions
    {
        [TestMethod]
        public void ToIPAddress()
        {
            var input = new byte[] { 0x4, 0xc0, 0xa8, 0x00, 0x01 };

            var address = MultiAddress.Create(input);

            var ipAddress = address.ToIPAddress();
            ipAddress.ToString().ShouldBe("192.168.0.1");
        }

        [TestMethod]
        public void ToIPEndPoint()
        {
            var input = new byte[] { 0x04, 0x00, 0x00, 0x00, 0x00, 0x06, 0x04, 0xd2 };

            var address = MultiAddress.Create(input);

            var ipEndPoint = address.ToIPEndPoint();
            ipEndPoint.Address.ToString().ShouldBe("0.0.0.0");
            ipEndPoint.Port.ShouldBe(1234);
        }
    }
}
