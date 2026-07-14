using System;
using System.Runtime.Serialization;

namespace Model
{
    /// <summary>
    /// Resultado de la comprobación de salud del servicio. Es el equivalente a los
    /// health checks del proyecto REST y no requiere autenticación.
    /// </summary>
    [DataContract]
    public class HealthResponse : Response
    {
        /// <summary>Verdadero si la base de datos respondió al sondeo.</summary>
        [DataMember]
        public Boolean DatabaseReachable { get; set; }
    }
}
