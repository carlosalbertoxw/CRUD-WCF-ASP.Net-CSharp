using System;
using System.Runtime.Serialization;

namespace Model
{
    /// <summary>
    /// SOAP no tiene códigos de estado como HTTP, así que cada respuesta lleva un
    /// <see cref="ResponseStatus"/> legible por máquina además del mensaje para el
    /// usuario. Es el equivalente a los 200/400/401/404/429 de la versión REST.
    /// </summary>
    [DataContract]
    public enum ResponseStatus
    {
        [EnumMember] Ok = 0,
        [EnumMember] ValidationError = 1,
        [EnumMember] Unauthorized = 2,
        [EnumMember] NotFound = 3,
        [EnumMember] RateLimited = 4,
        [EnumMember] Error = 5
    }

    /// <summary>Respuesta base: estado, bandera de éxito y mensaje.</summary>
    [DataContract]
    public class Response
    {
        [DataMember]
        public ResponseStatus Status { get; set; }

        /// <summary>Verdadero solo cuando <see cref="Status"/> es <see cref="ResponseStatus.Ok"/>.</summary>
        [DataMember]
        public Boolean Success
        {
            get { return Status == ResponseStatus.Ok; }
            set { /* solo lectura efectiva; el setter existe para el serializador */ }
        }

        [DataMember]
        public String Message { get; set; }
    }
}
