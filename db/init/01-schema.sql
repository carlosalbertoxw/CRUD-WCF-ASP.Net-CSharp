-- Esquema de la base de datos de notas. Se aplica automáticamente la primera vez
-- que arranca el contenedor (docker-entrypoint-initdb.d). Convención: todas las
-- fechas se guardan en UTC (DATETIME, no TIMESTAMP, para evitar el límite de 2038
-- y la conversión por zona horaria de sesión).
SET time_zone = '+00:00';

-- API keys: el secreto se guarda hasheado (SHA-256, BINARY(32)) e identificado por
-- key_id. El cliente presenta "<key_id>.<secreto>". Permite varias keys (una por
-- cliente), revocarlas (revoked_at) y caducarlas (expires_at) individualmente.
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

-- Notas: cada una pertenece a una API key (owner_key_id). MEDIUMTEXT (16 MB) es de
-- sobra para el límite de 100.000 caracteres de la aplicación.
CREATE TABLE IF NOT EXISTS notes (
  id           INT          NOT NULL AUTO_INCREMENT,
  owner_key_id VARCHAR(64)  NOT NULL,
  title        VARCHAR(250) NOT NULL,
  text         MEDIUMTEXT   NULL,
  created_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  -- La FK crea implícitamente el índice por owner_key_id que usa el listado por
  -- cliente, e impide borrar una key que aún tiene notas (se revoca, no se borra).
  CONSTRAINT fk_notes_owner FOREIGN KEY (owner_key_id) REFERENCES api_keys (key_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

-- InnoDB exige crear el índice FULLTEXT en una sentencia aparte.
ALTER TABLE notes ADD FULLTEXT INDEX ftx_notes_title_text (title, text);
