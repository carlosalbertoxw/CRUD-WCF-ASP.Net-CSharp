using System;
using System.Diagnostics;

namespace Utilities
{
    /// <summary>
    /// Logging del servicio sobre <see cref="TraceSource"/> (el mecanismo de
    /// logging integrado en .NET Framework, configurable por
    /// <c>system.diagnostics</c> en el Web.config, sin dependencias externas).
    /// Sustituye a los <c>Debug.WriteLine</c> dispersos por un único punto con
    /// niveles (Error/Warning/Information) y listeners configurables.
    /// </summary>
    public static class Log
    {
        private static readonly TraceSource Source = new TraceSource("Notes", SourceLevels.Warning);

        public static void Error(String message)
        {
            Source.TraceEvent(TraceEventType.Error, 0, message);
        }

        public static void Error(String message, Exception ex)
        {
            Source.TraceEvent(TraceEventType.Error, 0, "{0} | {1}", message, Describe(ex));
        }

        public static void Warning(String message)
        {
            Source.TraceEvent(TraceEventType.Warning, 0, message);
        }

        public static void Warning(String message, Exception ex)
        {
            Source.TraceEvent(TraceEventType.Warning, 0, "{0} | {1}", message, Describe(ex));
        }

        public static void Information(String message)
        {
            Source.TraceEvent(TraceEventType.Information, 0, message);
        }

        private static String Describe(Exception ex)
        {
            if (ex == null)
            {
                return "(sin excepción)";
            }
            return ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace;
        }
    }
}
