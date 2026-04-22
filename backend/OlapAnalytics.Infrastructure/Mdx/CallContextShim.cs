using System;
using System.Collections.Concurrent;
using System.Threading;

namespace System.Runtime.Remoting.Messaging
{
    /// <summary>
    /// Shim for System.Runtime.Remoting.Messaging.CallContext to allow legacy ADOMD.NET
    /// client libraries to function in modern .NET (.NET Core / 5 / 6 / 7 / 8 / 10).
    /// </summary>
    public static class CallContext
    {
        static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

        public static void LogicalSetData(string name, object data) =>
            state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

        public static object? LogicalGetData(string name) =>
            state.TryGetValue(name, out AsyncLocal<object>? data) ? data.Value : null;

        public static void FreeNamedDataSlot(string name)
        {
            if (state.TryGetValue(name, out AsyncLocal<object>? data))
            {
                data.Value = null!;
            }
        }
    }
}
