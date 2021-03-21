using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Multiformats.Net.Test
{
    [TestClass]
    public class MultiAddress_ToString
    {
        [TestMethod]
        public void ToStringIp4()
        {
            var input = new byte[] {0x4, 0xc0, 0xa8, 0x00, 0x01};

            var address = MultiAddress.Create(input);

            var output = address.ToString();
            output.ShouldBe("/ip4/192.168.0.1");
        }
    }
}
