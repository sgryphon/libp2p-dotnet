using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Multiformats.Net.Test
{
    [TestClass]
    public class MultiAddress_Parse
    {
        [TestMethod]
        public void ParseIp4()
        {
            var input = "/ip4/192.168.0.1";

            var address = MultiAddress.Parse(input);

            var bytes = address.AsSpan().ToArray();
            bytes.ShouldBe(new byte[] {0x4, 0xc0, 0xa8, 0x00, 0x01});
        }

        [TestMethod]
        public void ParseIp4AndPort()
        {
            var input = "/ip4/0.0.0.0/tcp/1234";

            var address = MultiAddress.Parse(input);

            var bytes = address.AsSpan().ToArray();
            bytes.ShouldBe(new byte[] {0x04, 0x00, 0x00, 0x00, 0x00, 0x06, 0x04, 0xd2});
        }

        [TestMethod]
        public void ParseIp6()
        {
            var input = "/ip6/abcd:0:1:2:3:4:5:6";

            var address = MultiAddress.Parse(input);

            var bytes = address.AsSpan().ToArray();
            bytes.ShouldBe(new byte[]
            {
                0x29, 0xab, 0xcd, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00, 0x06
            });
        }

        [TestMethod]
        public void ParseTcp()
        {
            var input = "/tcp/5001";

            var address = MultiAddress.Parse(input);

            var bytes = address.AsSpan().ToArray();
            bytes.ShouldBe(new byte[] {0x6, 0x13, 0x89});
        }
    }
}
