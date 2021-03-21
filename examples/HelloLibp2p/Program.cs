﻿using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;
using Libp2p.Net.Transport.Tcp;
using Multiformats.Net;

namespace HelloLibp2p
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "listen")
            {
                Console.WriteLine("Press CTRL+C to exit");
                var listenAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                var client = new Libp2pClient(new TcpTransport());
                var listener = await client.ListenAsync(listenAddress);
                Console.WriteLine("Listening");
                var connection = await listener.AcceptConnectionAsync();
                Console.WriteLine("Accepted connection");
                var stream = connection.GetStream();
                var textReader = new StreamReader(stream);
                
                while (true)
                {
                    var line = await textReader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine(line);
                    }
                    /*
                    var b = stream.ReadByte();
                    if (b >= 0)
                    {   
                        Console.Write(b.ToString("x2"));
                    }
                    */

                    await Task.Delay(TimeSpan.Zero);
                }
            }
            else if (args.Length > 0 && args[0] == "connect")
            {
                var connectAddress = MultiAddress.Parse("/ip4/127.0.0.1/tcp/5001");
                var client = new Libp2pClient(new TcpTransport());
                var connection = await client.ConnectAsync(connectAddress);
                var stream = connection.GetStream();
                var textWriter = new StreamWriter(stream);
                
                Console.WriteLine("Press Enter to send Hello");
                Console.ReadLine();
                var sendBytes = Encoding.UTF8.GetBytes ("Hello World!\n");
                await stream.WriteAsync(sendBytes);
                await stream.FlushAsync();
                //await textWriter.WriteLineAsync("Hello World!");
                //await textWriter.FlushAsync();
                
                Console.WriteLine("Press Enter to send Goodbye");
                Console.ReadLine();
                await textWriter.WriteLineAsync("Goodbye P2P");
                await textWriter.FlushAsync();

                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }
    }
}