using System;

namespace Utilities
{
    /// <summary>
    /// Validación de los datos de una nota. Refleja los límites de la versión REST
    /// (título obligatorio de hasta 250 caracteres, contenido de hasta 100.000) y
    /// los de la propia base de datos (VARCHAR(250) / MEDIUMTEXT).
    /// </summary>
    public static class Validation
    {
        public const Int32 TITLE_MAX_LENGTH = 250;
        public const Int32 TEXT_MAX_LENGTH = 100000;

        /// <summary>
        /// Devuelve el mensaje del primer error encontrado, o null si el título y
        /// el contenido son válidos.
        /// </summary>
        public static String ValidateNote(String title, String text)
        {
            if (String.IsNullOrWhiteSpace(title))
            {
                return Message.ERROR_TITLE_REQUIRED;
            }
            if (title.Length > TITLE_MAX_LENGTH)
            {
                return Message.ERROR_TITLE_TOO_LONG;
            }
            if (text != null && text.Length > TEXT_MAX_LENGTH)
            {
                return Message.ERROR_TEXT_TOO_LONG;
            }
            return null;
        }
    }
}
