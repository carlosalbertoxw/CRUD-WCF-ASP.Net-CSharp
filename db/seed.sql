-- Datos SOLO para desarrollo local. NO ejecutar en producción.
-- Crea la API key de desarrollo y un par de notas de ejemplo suyas.
--
-- API key de desarrollo:  local-dev.dev-secret
--   (key_hash = SHA-256 de "dev-secret")
--
-- Aplicar manualmente, por ejemplo:
--   docker exec -i notes-mysql mysql -uroot -proot_password notes < db/seed.sql
SET time_zone = '+00:00';

INSERT INTO api_keys (key_id, key_hash, client_name) VALUES
  ('local-dev', UNHEX(SHA2('dev-secret', 256)), 'Desarrollo local')
ON DUPLICATE KEY UPDATE client_name = VALUES(client_name);

INSERT INTO notes (owner_key_id, title, text) VALUES
  ('local-dev', 'Nota de ejemplo 1', 'Contenido de la primera nota de ejemplo.'),
  ('local-dev', 'Nota de ejemplo 2', 'Contenido de la segunda nota de ejemplo.');
