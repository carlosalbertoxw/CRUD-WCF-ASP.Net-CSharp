using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Utilities;
using Xunit;

namespace Notes.UnitTests
{
    /// <summary>
    /// Pruebas del logging sobre TraceSource. Para observar lo que emite <see cref="Log"/>
    /// se engancha un listener en memoria a su TraceSource, que es un campo privado:
    /// se alcanza por reflexión porque la clase no expone la fuente y añadir un punto
    /// de extensión solo para las pruebas cambiaría el código de producción.
    /// </summary>
    public class LogTests : IDisposable
    {
        private readonly TraceSource source;
        private readonly CapturingListener listener;

        public LogTests()
        {
            FieldInfo field = typeof(Log).GetField("Source", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(field);

            source = (TraceSource)field.GetValue(null);
            listener = new CapturingListener();
            source.Listeners.Add(listener);
        }

        public void Dispose()
        {
            source.Listeners.Remove(listener);
        }

        [Fact]
        public void Error_with_exception_reports_the_message_type_and_text()
        {
            Log.Error("fallo al guardar", new InvalidOperationException("boom"));

            string entry = Assert.Single(listener.Events);
            Assert.Contains("Error", entry);
            Assert.Contains("fallo al guardar", entry);
            Assert.Contains("InvalidOperationException", entry);
            Assert.Contains("boom", entry);
        }

        [Fact]
        public void Error_with_a_null_exception_does_not_blow_up()
        {
            Log.Error("fallo sin excepcion", null);

            string entry = Assert.Single(listener.Events);
            Assert.Contains("fallo sin excepcion", entry);
            Assert.Contains("(sin excepción)", entry);
        }

        [Fact]
        public void Information_is_filtered_out_at_the_default_level()
        {
            // La fuente se crea con SourceLevels.Warning, así que lo informativo no sale.
            Log.Information("detalle irrelevante");

            Assert.Empty(listener.Events);
        }

        /// <summary>Listener en memoria que guarda lo que emite la fuente.</summary>
        private sealed class CapturingListener : TraceListener
        {
            public readonly List<string> Events = new List<string>();

            public override void TraceEvent(TraceEventCache eventCache, string source,
                TraceEventType eventType, int id, string message)
            {
                Events.Add(eventType + ": " + message);
            }

            public override void TraceEvent(TraceEventCache eventCache, string source,
                TraceEventType eventType, int id, string format, params object[] args)
            {
                Events.Add(eventType + ": " +
                    (args == null ? format : string.Format(format, args)));
            }

            public override void Write(string message)
            {
            }

            public override void WriteLine(string message)
            {
            }
        }
    }
}
