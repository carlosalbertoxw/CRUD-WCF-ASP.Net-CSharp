using System;
using System.Runtime.Serialization;

namespace Model
{
    /// <summary>
    /// Cuerpo de las operaciones de creación y actualización de notas. Se separa
    /// de <see cref="Note"/> para que el cliente no pueda intentar fijar el id o
    /// las fechas: esos valores los gobierna el servidor.
    /// </summary>
    [DataContract]
    public class NoteRequest
    {
        [DataMember]
        public String Title { get; set; }
        [DataMember]
        public String Text { get; set; }
    }
}
