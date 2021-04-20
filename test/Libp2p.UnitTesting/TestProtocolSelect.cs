using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Libp2p.Net;

namespace Libp2p.UnitTesting
{
    public class TestProtocolSelect : Dictionary<Type, object>, IProtocolSelect
    {
        public Task<T?> SelectProtocolAsync<T>(IPipeline pipeline, T protocol, CancellationToken cancellationToken = default) where T : class, IProtocol
        {
            TryGetValue(typeof(T), out var value);
            return Task.FromResult(value as T);
        }

        public Task<T?> ListenProtocolAsync<T>(IPipeline pipeline, IList<T> protocols, CancellationToken cancellationToken = default) where T : class, IProtocol
        {
            TryGetValue(typeof(T), out var value);
            return Task.FromResult(value as T);
        }
    }
}
