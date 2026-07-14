using System;

namespace Utilities
{
    /// <summary>Mensajes de usuario que acompañan a las respuestas del servicio.</summary>
    public class Message
    {
        public static readonly String ERROR_CONNECTION_DB = "Ocurrió un error al conectar con la base de datos.";
        public static readonly String SUCCESSFUL_SAVE = "Datos guardados exitosamente.";
        public static readonly String ERROR_SAVE = "Ocurrió un error al guardar los datos.";
        public static readonly String SUCCESSFUL_UPDATE = "Datos actualizados exitosamente.";
        public static readonly String ERROR_UPDATE = "Ocurrió un error al actualizar los datos.";
        public static readonly String SUCCESSFUL_DELETE = "Datos eliminados exitosamente.";
        public static readonly String ERROR_DELETE = "Ocurrió un error al eliminar los datos.";
        public static readonly String SUCCESSFUL_CONSULT = "Consulta realizada exitosamente.";
        public static readonly String ERROR_CONSULT = "Ocurrió un error al consultar los datos.";
        public static readonly String ERROR_PROCESSING_DATA = "Ocurrió un error al procesar los datos.";

        // Autenticación / autorización.
        public static readonly String ERROR_SESSION_VALIDATION = "API key inválida, revocada o expirada.";
        public static readonly String ERROR_AUTH_FORMAT = "Formato de API key inválido; se espera '<key_id>.<secreto>'.";

        // Validación de entrada.
        public static readonly String ERROR_NOTE_REQUIRED = "La nota es obligatoria.";
        public static readonly String ERROR_TITLE_REQUIRED = "El título es obligatorio.";
        public static readonly String ERROR_TITLE_TOO_LONG = "El título no puede superar los 250 caracteres.";
        public static readonly String ERROR_TEXT_TOO_LONG = "El contenido no puede superar los 100.000 caracteres.";

        // Recurso / límites.
        public static readonly String ERROR_NOTE_NOT_FOUND = "La nota no existe o no pertenece al cliente autenticado.";
        public static readonly String ERROR_RATE_LIMITED = "Se superó el límite de peticiones. Intente nuevamente más tarde.";

        // Salud.
        public static readonly String HEALTH_OK = "El servicio está operativo.";
        public static readonly String HEALTH_DB_DOWN = "El servicio no puede alcanzar la base de datos.";
    }
}
