using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Model
{
    /// <summary>
    /// Página de notas (resúmenes) con paginación por keyset: para pedir la
    /// siguiente página se envía <c>afterId = NextAfterId</c>. Costo constante en
    /// la base de datos sin importar la profundidad (a diferencia de OFFSET).
    /// </summary>
    [DataContract]
    public class NoteListResponse : Response
    {
        [DataMember]
        public List<NoteSummary> Items { get; set; }

        [DataMember]
        public Int32 PageSize { get; set; }

        [DataMember]
        public Int64 TotalCount { get; set; }

        /// <summary>Cursor para la siguiente página, o null si no hay más resultados.</summary>
        [DataMember]
        public Nullable<Int32> NextAfterId { get; set; }
    }
}
