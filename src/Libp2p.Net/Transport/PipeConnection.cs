﻿using System.IO.Pipelines;

namespace Libp2p.Net.Transport
{
    public class PipeConnection : IConnection
    {
        public PipeConnection(PipeReader input, PipeWriter output)
        {
            Input = input;
            Output = output;
        }

        public PipeReader Input { get; }
        public PipeWriter Output { get; }

        public void Dispose()
        {
        }
    }
}
