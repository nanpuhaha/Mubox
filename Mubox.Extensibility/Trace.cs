using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mubox.Extensibility
{
    public class Trace
        : MarshalByRefObject
    {
        internal static Trace Instance { get; set; }

        internal static IServiceProvider ServiceProvider { get; set; }

        static Trace()
        {
            Instance = new Trace();
        }

        private static TraceSource _trace;

        private static int traceId = 0;

        public Trace()
        {
            if (_trace == null)
            {
                _trace = new TraceSource(AppDomain.CurrentDomain.FriendlyName ?? "Default");
                var listeners = System.Diagnostics.Trace.Listeners
                    .OfType<TraceListener>()
                    .Where(l => l.GetType().FullName.StartsWith("Mubox."))
                    .ToList();
                listeners
                    .ForEach(listener => _trace.Listeners.Add(listener));

                _trace.Switch = new SourceSwitch("All", "All");
            }
        }

        [Conditional("DEBUG")]
        public void Log(string message, TraceEventType traceEventType = TraceEventType.Verbose)
        {
            _trace.TraceEvent(traceEventType, Interlocked.Increment(ref traceId), message);
        }

        [Conditional("DEBUG")]
        public void Log(Exception ex, TraceEventType traceEventType = TraceEventType.Error)
        {
            new
            {
                Exception = ex.GetType().Name,
                Message = ex.Message ?? "(null)",
                StackTrace = ex.StackTrace ?? "(null)",
            }.Log(traceEventType);
        }

        [Conditional("DEBUG")]
        public void Log<T>(T data, TraceEventType traceEventType = TraceEventType.Verbose)
            where T : class
        {
            _trace.TraceData(traceEventType, Interlocked.Increment(ref traceId), data);
        }
    }

    public static class TraceExtensions
    {
        public static void Log(this string message, TraceEventType traceEventType = TraceEventType.Verbose)
        {
            ConsoleWrite(Convert.ToString(traceEventType), message);
            Trace.Instance.Log(message, traceEventType);
        }

        private static void ConsoleWrite(string category, string message)
        {
            try
            {
                if (Trace.ServiceProvider != null)
                {
                    var console = Trace.ServiceProvider.GetService(typeof(IConsoleService)) as IConsoleService;
                    if (console != null)
                    {
                        console.WriteLine(category, message);
                    }
                }
            }
            catch
            {
            }
        }

        public static void Log(this Exception ex, TraceEventType traceEventType = TraceEventType.Error)
        {
            Trace.Instance.Log(ex, traceEventType);
        }

        public static void Log<T>(this T data, TraceEventType traceEventType = TraceEventType.Verbose)
            where T : class
        {
            Trace.Instance.Log<T>(data, traceEventType);
        }
    }
}
