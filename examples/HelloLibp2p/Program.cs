using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Libp2p.Net;
using Libp2p.Net.Cryptography;
using Libp2p.Net.Protocol;
using Libp2p.Net.Streams;
using Libp2p.Net.Transport.Tcp;
using Multiformats.Net;

namespace HelloLibp2p
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var selectors = new IProtocolSelect[] {new MultistreamSelect1()};
            var encryptors = new IEncryptionProtocol[] {new Plaintext()};
            var multiplexers = new IMultiplexProtocol[] {new Mplex67()};
            var upgrader = (IConnectionUpgrader)new ConnectionUpgrader(selectors, encryptors, multiplexers);
                
            var transports = new ITransport[] {new TcpTransport()};
            using var client = new Libp2pClient(transports, upgrader);
            
            if (args.Length > 0 && args[0] == "listen")
            {
                Console.WriteLine("Press CTRL+C to exit");
                var listenAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                await client.ListenAsync(listenAddress);
                Console.WriteLine("Listening on {0}", listenAddress);
                using var connection = await client.AcceptAsync();
                Console.WriteLine("Accepted {0} connection from {1} on {2}", connection.Direction,
                    connection.RemoteAddress, connection.LocalAddress);
                var (pipeline, protocolIdentifier) = await connection.AcceptAsync(new [] {"hello/1.0"});
                Console.WriteLine("Accepted pipeline for {0}", protocolIdentifier);

                while (true)
                {
                    var result = await pipeline.Input.ReadAsync();
                    var buffer = result.Buffer;
                    ReadLine(ref buffer);
                    pipeline.Input.AdvanceTo(buffer.Start, buffer.End);
                    if (result.IsCompleted) break;
                    await Task.Delay(TimeSpan.Zero);
                }
            }

            if (args.Length > 0 && args[0] == "connect")
            {
                var connectAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                using var connection = await client.ConnectAsync(connectAddress);
                Console.WriteLine("Connected");
                Console.WriteLine("Connected {0} to {1} from {2}", connection.Direction,
                    connection.RemoteAddress, connection.LocalAddress);
                var (pipeline, protocolIdentifier) = await connection.ConnectAsync("hello/1.0");
                Console.WriteLine("Connected pipeline for {0}", protocolIdentifier);

                Console.WriteLine("Press Enter to send Hello");
                Console.ReadLine();
                var sendBytes = Encoding.UTF8.GetBytes("Hello World!\n");
                await pipeline.Output.WriteAsync(sendBytes);

                Console.WriteLine("Press Enter to send Goodbye");
                Console.ReadLine();
                var sendBytes2 = Encoding.UTF8.GetBytes("Goodbye P2P\n");
                await pipeline.Output.WriteAsync(sendBytes2);

                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }

        private static void ReadLine(ref ReadOnlySequence<byte> buffer)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);

            while (!sequenceReader.End)
            {
                while (sequenceReader.TryReadTo(out ReadOnlySpan<byte> line, (byte)'\n'))
                {
                    var s = Encoding.UTF8.GetString(line);
                    Console.WriteLine(s);
                }

                buffer = buffer.Slice(sequenceReader.Position);
                sequenceReader.Advance(buffer.Length);
            }
        }
    }
}
