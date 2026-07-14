using System.Runtime.Serialization;

namespace Model
{
    /// <summary>Respuesta que transporta una nota individual con su contenido.</summary>
    [DataContract]
    public class NoteResponse : Response
    {
        [DataMember]
        public Note Note { get; set; }
    }
}
