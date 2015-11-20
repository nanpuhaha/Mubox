using Mubox.Extensibility;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace System
{
    public static class LoggingExtensions
    {
        internal static IServiceProvider ServiceProvider { get; set; }
        private static TraceSource _trace;
        private static int _id;

        static LoggingExtensions()
        {
            _trace = new TraceSource("Log", SourceLevels.All);
            System.Diagnostics.Trace.Listeners
                .OfType<TraceListener>()
                .Where(l => l.GetType().FullName.StartsWith("Mubox."))
                .ToList()
                .ForEach(listener => _trace.Listeners.Add(listener));
            _trace.Switch = new SourceSwitch("All", "All");
            _id = 0;
        }

        private static Regex _normalizeRegex = new Regex(@"[ \t\n\r]+");

        private static string Normalize(string input)
        {
            var pass1 = _normalizeRegex.Replace(input, " ");
            return _normalizeRegex.Replace(pass1, " ");
        }

        private static void ConsoleWrite(string category, string message)
        {
            try
            {
                var serviceProvider = LoggingExtensions.ServiceProvider;
                if (serviceProvider != null)
                {
                    var console = serviceProvider.GetService(typeof(IConsoleService)) as IConsoleService;
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

        public static void Log<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Verbose, normalize);
        }

        public static void Log<T>(this T data, TraceEventType type, bool normalize = true)
            where T : class
        {
            if (data is Exception && type == TraceEventType.Verbose)
            {
                type = TraceEventType.Error;
            }
            var id = Interlocked.Increment(ref _id);
            var message = normalize
                ? Normalize(Convert.ToString(data))
                : Convert.ToString(data);
            _trace.TraceData(
                type,
                id,
                message);
            ConsoleWrite(Convert.ToString(type), message);
        }

        public static void LogVerbose<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Verbose, normalize);
        }

        public static void LogInfo<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Information, normalize);
        }

        public static void LogWarn<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Warning, normalize);
        }

        public static void LogError<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Error, normalize);
        }

        public static void LogCritical<T>(this T data, bool normalize = true)
            where T : class
        {
            data.Log(TraceEventType.Critical, normalize);
        }

        private static string FullStackTrace(Exception ex)
        {
            var sb = new StringBuilder();
            while (ex != null)
            {
                if (ex.StackTrace != null)
                {
                    sb.AppendLine(ex.StackTrace);
                }
                ex = ex.InnerException;
                if (ex != null)
                {
                    sb.AppendLine("==(" + ex.GetType().FullName + "|" + (ex.Message ?? "") + ")==");
                }
            }
            return sb.Replace(Environment.NewLine, "..").ToString();
        }
    }
}