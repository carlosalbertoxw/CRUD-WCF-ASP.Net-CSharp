using System;
using System.Threading.Tasks;
using Data;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit;

namespace Notes.IntegrationTests
{
    /// <summary>
    /// Levanta un MySQL 8.4 efímero con Testcontainers, le aplica el esquema real y
    /// publica la cadena de conexión a la capa de datos vía la variable de entorno
    /// NOTES_DB_CONNECTION. Requiere Docker en la máquina que corre las pruebas.
    /// </summary>
    public class MySqlFixture : IAsyncLifetime
    {
        private readonly MySqlContainer container;

        public MySqlFixture()
        {
            container = new MySqlBuilder()
                .WithImage("mysql:8.4")
                .WithDatabase("notes")
                .WithUsername("notes")
                .WithPassword("notes")
                // Convención del proyecto: el servidor guarda las fechas en UTC.
                .WithCommand("--default-time-zone=+00:00")
                .Build();
        }

        /// <summary>Cadena de conexión de la aplicación al contenedor.</summary>
        public string ConnectionString { get; private set; }

        public async Task InitializeAsync()
        {
            await container.StartAsync();
            await container.ExecScriptAsync(Schema);

            // AllowPublicKeyRetrieval: el usuario usa caching_sha2_password y la
            // conexión al contenedor es sin TLS.
            ConnectionString = container.GetConnectionString() + ";AllowPublicKeyRetrieval=True";
            Environment.SetEnvironmentVariable(DataAccess.ConnectionEnvVar, ConnectionString);
        }

        public Task DisposeAsync()
        {
            Environment.SetEnvironmentVariable(DataAccess.ConnectionEnvVar, null);
            return container.DisposeAsync().AsTask();
        }

        /// <summary>Inserta una API key (secreto hasheado con SHA-256) para las pruebas.</summary>
        public void InsertApiKey(string keyId, string secret,
            DateTime? expiresAt = null, DateTime? revokedAt = null)
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(
                    "INSERT INTO api_keys (key_id, key_hash, client_name, expires_at, revoked_at) " +
                    "VALUES (@id, UNHEX(SHA2(@secret, 256)), @name, @expires, @revoked);", connection))
                {
                    command.Parameters.AddWithValue("@id", keyId);
                    command.Parameters.AddWithValue("@secret", secret);
                    command.Parameters.AddWithValue("@name", "Cliente " + keyId);
                    command.Parameters.AddWithValue("@expires", (object)expiresAt ?? DBNull.Value);
                    command.Parameters.AddWithValue("@revoked", (object)revokedAt ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        private const string Schema = @"
SET time_zone = '+00:00';
CREATE TABLE IF NOT EXISTS api_keys (
  key_id       VARCHAR(64)  NOT NULL,
  key_hash     BINARY(32)   NOT NULL,
  client_name  VARCHAR(100) NOT NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revoked_at   DATETIME     NULL DEFAULT NULL,
  expires_at   DATETIME     NULL DEFAULT NULL,
  last_used_at DATETIME     NULL DEFAULT NULL,
  PRIMARY KEY (key_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
CREATE TABLE IF NOT EXISTS notes (
  id           INT          NOT NULL AUTO_INCREMENT,
  owner_key_id VARCHAR(64)  NOT NULL,
  title        VARCHAR(250) NOT NULL,
  text         MEDIUMTEXT   NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  CONSTRAINT fk_notes_owner FOREIGN KEY (owner_key_id) REFERENCES api_keys (key_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;
ALTER TABLE notes ADD FULLTEXT INDEX ftx_notes_title_text (title, text);
";
    }

    [CollectionDefinition("mysql")]
    public class MySqlCollection : ICollectionFixture<MySqlFixture>
    {
    }
}
