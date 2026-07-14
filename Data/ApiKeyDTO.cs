using System;
using Model;
using MySqlConnector;
using Utilities;

namespace Data
{
    /// <summary>
    /// Acceso a datos de las API keys. Las keys se guardan hasheadas (SHA-256,
    /// BINARY(32)) e identificadas por <c>key_id</c>, lo que permite tener varias
    /// (una por cliente), revocarlas (<c>revoked_at</c>) y caducarlas
    /// (<c>expires_at</c>) de forma individual.
    /// </summary>
    public class ApiKeyDTO
    {
        private readonly DataAccess dataAccess;

        public ApiKeyDTO()
        {
            dataAccess = new DataAccess();
        }

        /// <summary>
        /// Devuelve la key activa (ni revocada ni expirada) con ese id, o null.
        /// </summary>
        public ApiKey getActiveKey(String keyId)
        {
            MySqlConnection connection = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return null;
                }
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT key_id, key_hash, client_name FROM api_keys " +
                    "WHERE key_id = @keyId AND revoked_at IS NULL " +
                    "AND (expires_at IS NULL OR expires_at > UTC_TIMESTAMP());", connection))
                {
                    command.Parameters.AddWithValue("@keyId", keyId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }
                        return new ApiKey
                        {
                            KeyId = reader.GetString(0),
                            KeyHash = (Byte[])reader.GetValue(1),
                            ClientName = reader.GetString(2)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al consultar la API key.", ex);
                return null;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }

        /// <summary>
        /// Registra el uso de la key. Actualización laxa: solo escribe si pasó más
        /// de una hora desde el último registro, para no convertir cada petición en
        /// una escritura.
        /// </summary>
        public void registerUse(String keyId)
        {
            MySqlConnection connection = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return;
                }
                using (MySqlCommand command = new MySqlCommand(
                    "UPDATE api_keys SET last_used_at = UTC_TIMESTAMP() " +
                    "WHERE key_id = @keyId AND (last_used_at IS NULL " +
                    "OR last_used_at < UTC_TIMESTAMP() - INTERVAL 1 HOUR);", connection))
                {
                    command.Parameters.AddWithValue("@keyId", keyId);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Telemetría de mejor esfuerzo: no debe tumbar una autenticación válida.
                Log.Warning("No se pudo registrar el uso de la key.", ex);
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }
    }
}
