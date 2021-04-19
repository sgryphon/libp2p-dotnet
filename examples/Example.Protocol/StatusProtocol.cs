
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Example.Protocol.Message;
using Libp2p.Net;
using Multiformats.Net;

namespace Example.Protocol
{
    public class StatusProtocol : IProtocol
    {
        private readonly BeaconState _beaconState;

        public StatusProtocol(BeaconState beaconState)
        {
            _beaconState = beaconState;
        }
        
        public string Identifier => "/eth2/beacon_chain/req/status/1/";
        
        public Task HandleAsync(IConnection connection, IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
        
        public async Task StartAsync(IConnection connection, IPipeline pipeline, CancellationToken cancellationToken = default)
        {
            var writeTask = WriteStatus(pipeline, cancellationToken);
            var readTask = ReadStatus(pipeline, cancellationToken);
            await Task.WhenAll(writeTask, readTask);
        }

        private async Task WriteStatus(IPipeline pipeline, CancellationToken cancellationToken)
        {
            var statusMessage = new StatusMessage() {HeadSlot = _beaconState.HeadSlot};
            var bytes = JsonSerializer.SerializeToUtf8Bytes(statusMessage);

            var memory = pipeline.Output.GetMemory(bytes.Length + 5);
            VarIntUtility.WriteVarInt(memory.Span, bytes.Length, out var lengthBytes);
            bytes.CopyTo(memory[lengthBytes..]);
            pipeline.Output.Advance(bytes.Length + lengthBytes);
            _ = await pipeline.Output.FlushAsync(cancellationToken);
        }

        private static async Task ReadStatus(IPipeline pipeline, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await pipeline.Input.ReadAsync(cancellationToken);
                if (result.Buffer.TryReadVarInt(out var lengthConsumed, out var length))
                {
                    if (result.Buffer.Length >= lengthConsumed + length)
                    {
                        // NOTE: For this example we are just inefficiently copying to bytes and deserialise
                        // Actual ETH2.0 doesn't use JSON either; it has it's own serialisation protocol.
                        var bytes = new byte[length];
                        result.Buffer.CopyTo(bytes);
                        pipeline.Input.AdvanceTo(result.Buffer.GetPosition(lengthConsumed + length));
                        var status = JsonSerializer.Deserialize<StatusMessage>(bytes);
                        await ProcessStatusAsync(status, cancellationToken);
                    }
                    else
                    {
                        pipeline.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    }
                }
                else
                {
                    pipeline.Input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                }
            }
        }

        private static async Task ProcessStatusAsync(StatusMessage status, CancellationToken cancellationToken)
        {
            // TODO: Compare and trigger blocks by range if needed (needs to open new Pipeline for Connection)
            await Task.Delay(0, cancellationToken);
            throw new NotImplementedException();
        }
    }
}
