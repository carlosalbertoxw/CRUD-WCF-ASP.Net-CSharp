using System;
using System.Configuration;
using MySqlConnector;
using Utilities;

namespace Data
{
    /// <summary>
    /// Punto único de acceso a la conexión con MySQL. La cadena de conexión no está
    /// incrustada en el código: se resuelve, en este orden, desde la variable de
    /// entorno <c>NOTES_DB_CONNECTION</c> (para inyectar el secreto en producción
    /// sin commitearlo) y, si no está, desde <c>ConnectionStrings["NotesDb"]</c> del
    /// Web.config. La aplicación se conecta con un usuario de mínimo privilegio.
    /// </summary>
    public class DataAccess
    {
        public const String ConnectionName = "NotesDb";
        public const String ConnectionEnvVar = "NOTES_DB_CONNECTION";

        /// <summary>Devuelve la cadena de conexión configurada o lanza si falta.</summary>
        public static String GetConnectionString()
        {
            // 1) Variable de entorno (gestor de secretos del entorno / Kubernetes).
            String fromEnv = Environment.GetEnvironmentVariable(ConnectionEnvVar);
            if (!String.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            // 2) Web.config (por defecto en desarrollo local).
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (setting == null || String.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "Falta la cadena de conexión: define la variable de entorno '" +
                    ConnectionEnvVar + "' o 'ConnectionStrings/NotesDb' en la configuración.");
            }
            return setting.ConnectionString;
        }

        public MySqlConnection openConnection()
        {
            MySqlConnection connection;
            try
            {
                connection = new MySqlConnection(GetConnectionString());
                connection.Open();
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo abrir la conexión a la base de datos.", ex);
                connection = null;
            }
            return connection;
        }

        public void closeConnection(MySqlConnection connection)
        {
            if (connection == null)
            {
                return;
            }
            try
            {
                connection.Close();
            }
            catch (Exception ex)
            {
                Log.Error("No se pudo cerrar la conexión a la base de datos.", ex);
            }
        }

        /// <summary>
        /// Sondeo de salud: intenta abrir la conexión y ejecutar <c>SELECT 1</c>.
        /// No lanza; devuelve simplemente si la base de datos está alcanzable.
        /// </summary>
        public Boolean canConnect()
        {
            MySqlConnection connection = null;
            try
            {
                connection = openConnection();
                if (connection == null)
                {
                    return false;
                }
                using (MySqlCommand command = new MySqlCommand("SELECT 1;", connection))
                {
                    command.ExecuteScalar();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Sondeo de salud fallido.", ex);
                return false;
            }
            finally
            {
                closeConnection(connection);
            }
        }
    }
}
