using Model;
using System;
using System.ServiceModel;

namespace CRUD_WCF_ASP.Net_CSharp
{
    /// <summary>
    /// Contrato del servicio de notas. La autenticación es por API key con formato
    /// "&lt;key_id&gt;.&lt;secreto&gt;", que se pasa en el parámetro <c>apiKey</c> de cada
    /// operación (salvo <c>Health</c>). Cada cliente solo ve y modifica sus notas.
    /// </summary>
    [ServiceContract]
    public interface INoteService
    {
        /// <summary>
        /// Liveness (no requiere API key): indica que el proceso está vivo y
        /// respondiendo, sin tocar la base de datos. Útil para sondeos frecuentes.
        /// </summary>
        [OperationContract]
        HealthResponse Live();

        /// <summary>
        /// Readiness (no requiere API key): indica si la base de datos está
        /// alcanzable, es decir, si el servicio puede atender peticiones reales.
        /// </summary>
        [OperationContract]
        HealthResponse Health();

        /// <summary>
        /// Lista las notas del cliente (id, título y fechas). Paginación por keyset:
        /// enviar <paramref name="afterId"/> = NextAfterId de la página anterior.
        /// <paramref name="pageSize"/> se acota a 1..100 (por defecto 20). Con
        /// <paramref name="search"/> se filtra por texto completo.
        /// </summary>
        [OperationContract]
        NoteListResponse List(String apiKey, Int32 afterId, Int32 pageSize, String search);

        /// <summary>Obtiene una nota del cliente con su contenido completo.</summary>
        [OperationContract]
        NoteResponse Get(String apiKey, Int32 id);

        /// <summary>Crea una nota y devuelve la nota resultante (con id y fechas).</summary>
        [OperationContract]
        NoteResponse Add(String apiKey, NoteRequest note);

        /// <summary>Actualiza una nota del cliente.</summary>
        [OperationContract]
        Response Update(String apiKey, Int32 id, NoteRequest note);

        /// <summary>Elimina una nota del cliente.</summary>
        [OperationContract]
        Response Delete(String apiKey, Int32 id);
    }
}
