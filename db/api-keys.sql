-- Administración y verificación de las API keys usadas por el servicio WCF.
-- El secreto NUNCA se guarda en claro: se almacena su SHA-256 (BINARY(32)).
-- El cliente presenta la key como  <key_id>.<secreto>.
--
-- Nota: el esquema (tablas `notes` y `api_keys`) lo aprovisionan las migraciones
-- del entorno; este script solo administra keys y verifica su estado.

SET time_zone = '+00:00';   -- Convención del proyecto: fechas en UTC.

-- --------------------------------------------------------------------------
-- Verificar que el esquema esperado existe.
-- --------------------------------------------------------------------------
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'notes' AND table_name IN ('notes', 'api_keys');

-- --------------------------------------------------------------------------
-- Crear una API key.
-- El cliente usará  "X-Api-Key equivalente":  clave-cliente-1.<secreto-largo>
-- Sustituí el secreto por uno aleatorio y largo (p. ej. 32+ caracteres).
-- --------------------------------------------------------------------------
INSERT INTO api_keys (key_id, key_hash, client_name, expires_at)
VALUES ('clave-cliente-1',
        UNHEX(SHA2('reemplazar-por-un-secreto-aleatorio-largo', 256)),
        'Nombre del cliente',
        UTC_TIMESTAMP() + INTERVAL 1 YEAR);   -- expires_at NULL = sin caducidad.

-- --------------------------------------------------------------------------
-- Key de desarrollo local (equivale a  local-dev.dev-secret).
-- --------------------------------------------------------------------------
INSERT INTO api_keys (key_id, key_hash, client_name)
VALUES ('local-dev', UNHEX(SHA2('dev-secret', 256)), 'Desarrollo local');

-- --------------------------------------------------------------------------
-- Revocar una key (se revoca, no se elimina: la FK de notes.owner_key_id
-- impide borrar una key con notas, y así se conserva la trazabilidad).
-- --------------------------------------------------------------------------
UPDATE api_keys SET revoked_at = UTC_TIMESTAMP() WHERE key_id = 'clave-cliente-1';

-- --------------------------------------------------------------------------
-- Listar keys y su estado.
-- --------------------------------------------------------------------------
SELECT key_id,
       client_name,
       created_at,
       expires_at,
       revoked_at,
       last_used_at,
       CASE
         WHEN revoked_at IS NOT NULL THEN 'revocada'
         WHEN expires_at IS NOT NULL AND expires_at <= UTC_TIMESTAMP() THEN 'expirada'
         ELSE 'activa'
       END AS estado
FROM api_keys
ORDER BY created_at;

-- --------------------------------------------------------------------------
-- Detectar keys sin uso en los últimos 90 días (candidatas a revocar).
-- --------------------------------------------------------------------------
SELECT key_id, client_name, last_used_at
FROM api_keys
WHERE revoked_at IS NULL
  AND (last_used_at IS NULL OR last_used_at < UTC_TIMESTAMP() - INTERVAL 90 DAY);
