using System;

namespace Libp2p.UnitTesting
{
    public class DiagnosticMessage
    {
        public DiagnosticMessage(DateTimeOffset timestamp, string listener, string key, object value)
        {
            Timestamp = timestamp;
            Listener = listener;
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public string Listener { get; }
        public DateTimeOffset Timestamp { get; }
        public object Value { get; }
    }
}
