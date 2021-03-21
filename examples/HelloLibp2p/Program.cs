using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Libp2p.Net;
using Libp2p.Net.Transport.Tcp;
using Multiformats.Net;

namespace HelloLibp2p
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "listen")
            {
                Console.WriteLine("Press CTRL+C to exit");
                var listenAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                var client = new Libp2pClient(new TcpTransport());
                using var listener = await client.ListenAsync(listenAddress);
                Console.WriteLine("Listening");
                using var connection = await listener.AcceptConnectionAsync();
                Console.WriteLine("Accepted connection");

                while (true)
                {
                    var result = await connection.Input.ReadAsync();
                    var buffer = result.Buffer;
                    ReadLine(ref buffer);
                    connection.Input.AdvanceTo(buffer.Start, buffer.End);
                    if (result.IsCompleted) break;
                    await Task.Delay(TimeSpan.Zero);
                }
            }

            if (args.Length > 0 && args[0] == "connect")
            {
                var connectAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                var client = new Libp2pClient(new TcpTransport());
                using var connection = await client.ConnectAsync(connectAddress);

                Console.WriteLine("Press Enter to send Hello");
                Console.ReadLine();
                var sendBytes = Encoding.UTF8.GetBytes("Hello World!\n");
                await connection.Output.WriteAsync(sendBytes);

                Console.WriteLine("Press Enter to send Goodbye");
                Console.ReadLine();
                var sendBytes2 = Encoding.UTF8.GetBytes("Goodbye P2P\n");
                await connection.Output.WriteAsync(sendBytes2);

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
