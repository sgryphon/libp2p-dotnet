using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Libp2p.UnitTesting
{
    public class TestDiagnosticCollector : IDisposable
    {
        private readonly List<IDisposable> _allListeners = new();

        static TestDiagnosticCollector()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        }

        public TestDiagnosticCollector()
        {
            SubscribeToAllLibp2pDiagnostics();
        }

        public IList<DiagnosticMessage> DiagnosticMessages { get; } = new List<DiagnosticMessage>();

        public void Dispose()
        {
            foreach (var listener in _allListeners)
            {
                listener.Dispose();
            }
        }

        public IReadOnlyList<DiagnosticMessage> GetExceptions()
        {
            return DiagnosticMessages.Where(x => x.Value is Exception).ToImmutableList();
        }

        private void SubscribeToAllLibp2pDiagnostics()
        {
            _allListeners.Add(DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                if (listener.Name.StartsWith("Libp2p."))
                {
                    _allListeners.Add(listener.Subscribe(kvp =>
                    {
                        DiagnosticMessages.Add(new DiagnosticMessage(DateTimeOffset.Now, listener.Name, kvp.Key,
                            kvp.Value));
                        if (kvp.Value is Activity activity)
                        {
                            if (kvp.Key.EndsWith(".Start"))
                            {
                                Console.WriteLine("{0}: {1} {2}-{3} {4}", kvp.Key,
                                    activity.OperationName,
                                    activity.TraceId, activity.SpanId, string.Concat(activity.Tags));
                            }
                            else
                            {
                                Console.WriteLine("{0}: {1}({2}) {3}-{4} {5}", kvp.Key,
                                    activity.OperationName, activity.Duration,
                                    activity.TraceId, activity.SpanId, string.Concat(activity.Tags));
                            }
                        }
                        else
                        {
                            var currentActivity = Activity.Current;
                            Console.WriteLine("{0}: {1} {2}-{3}", kvp.Key, kvp.Value, 
                                currentActivity?.TraceId, currentActivity?.SpanId);
                        }
                    }));
                }
            }));
        }
    }
}
