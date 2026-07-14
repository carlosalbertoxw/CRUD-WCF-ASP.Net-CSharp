-- Usuario de mínimo privilegio para la aplicación: solo DML (SELECT/INSERT/
-- UPDATE/DELETE), sin DDL. Un bug o inyección hipotética en la app no puede
-- alterar el esquema. El usuario con todos los privilegios sobre la BD (el que
-- crean las variables de entorno del contenedor) queda reservado para migraciones.
-- La contraseña usa mysql_native_password para máxima compatibilidad de clientes;
-- MySqlConnector soporta también caching_sha2_password sin problema.
CREATE USER IF NOT EXISTS 'notes_app'@'%' IDENTIFIED BY 'notes_app_password';
GRANT SELECT, INSERT, UPDATE, DELETE ON `notes`.* TO 'notes_app'@'%';
FLUSH PRIVILEGES;
