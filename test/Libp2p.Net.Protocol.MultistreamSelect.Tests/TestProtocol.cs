﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libp2p.Net.Protocol.Tests
{
    public class TestProtocol : IProtocol
    {
        public readonly IList<IConnection> Connections = new List<IConnection>();

        public string Name => "TestProtocol";

        public Task StartAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            Connections.Add(connection);
            return Task.CompletedTask;
        }
    }
}
