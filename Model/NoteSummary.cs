using System;
using System.Runtime.Serialization;

namespace Model
{
    /// <summary>
    /// Vista resumida para los listados: no arrastra el contenido completo de la
    /// nota (el texto se obtiene por id con <c>Get</c>).
    /// </summary>
    [DataContract]
    public class NoteSummary
    {
        [DataMember]
        public Int32 Id { get; set; }
        [DataMember]
        public String Title { get; set; }
        [DataMember]
        public DateTime CreatedAt { get; set; }
        [DataMember]
        public DateTime UpdatedAt { get; set; }
    }
}
