using System;
using System.Collections.Concurrent;

namespace Utilities
{
    /// <summary>
    /// Rate limiter de ventana fija por partición (típicamente la IP del cliente).
    /// En WCF el servicio se instancia por llamada, así que el estado se comparte
    /// mediante una instancia estática. Es un mejor esfuerzo en memoria: con varias
    /// réplicas cada una lleva su propia cuenta (para límites duros haría falta un
    /// almacén distribuido como Redis).
    /// </summary>
    public class RateLimiter
    {
        private class Window
        {
            public DateTime Start;
            public Int32 Count;
        }

        private readonly Int32 permitLimit;
        private readonly TimeSpan window;
        private readonly ConcurrentDictionary<String, Window> partitions =
            new ConcurrentDictionary<String, Window>();

        public RateLimiter(Int32 permitLimit, Int32 windowSeconds)
        {
            this.permitLimit = permitLimit > 0 ? permitLimit : 100;
            this.window = TimeSpan.FromSeconds(windowSeconds > 0 ? windowSeconds : 60);
        }

        /// <summary>Segundos de la ventana; útil para el encabezado Retry-After.</summary>
        public Int32 WindowSeconds
        {
            get { return (Int32)this.window.TotalSeconds; }
        }

        /// <summary>
        /// Registra una petición de la partición indicada y devuelve false si con
        /// ella se supera el límite dentro de la ventana actual.
        /// </summary>
        public Boolean IsAllowed(String partitionKey)
        {
            if (String.IsNullOrEmpty(partitionKey))
            {
                partitionKey = "unknown";
            }

            DateTime now = DateTime.UtcNow;
            Window state = this.partitions.GetOrAdd(partitionKey, _ => new Window { Start = now, Count = 0 });

            lock (state)
            {
                if (now - state.Start >= this.window)
                {
                    // Ventana expirada: se reinicia el conteo.
                    state.Start = now;
                    state.Count = 0;
                }

                state.Count++;
                return state.Count <= this.permitLimit;
            }
        }
    }
}
