# CRUD-WCF-ASP.Net-CSharp

Servicio **WCF (SOAP)** de notas en ASP.NET / C# (.NET Framework 4.7.2) respaldado
por MySQL. Un CRUD de notas con **autenticación por API key** (hasheada, con
revocación y expiración), **notas privadas por cliente**, **paginación por keyset**,
**búsqueda de texto completo**, **rate limiting** por IP, **validación de entrada**,
marcas de tiempo en UTC, **health checks** (liveness/readiness), **logging**
configurable y **pruebas de integración** contra un MySQL real.

## Arquitectura

Solución por capas:

```
├── Model/        # Contratos de datos: Note, NoteRequest, NoteSummary,
│                 #   NoteListResponse (paginada), NoteResponse, HealthResponse,
│                 #   Response (+ ResponseStatus) y ApiKey.
├── Utilities/    # Seguridad de API keys (SHA-256 + comparación en tiempo
│                 #   constante), rate limiter en memoria, validación, mensajes y
│                 #   logging (TraceSource).
├── Data/         # Acceso a datos con MySqlConnector: DataAccess (conexión y
│                 #   sondeo de salud), NoteDTO y ApiKeyDTO. Consultas
│                 #   parametrizadas, acotadas al cliente dueño.
├── CRUD-WCF-ASP.Net-CSharp/   # Servicio WCF: contrato INoteService,
│                 #   implementación NoteService, Authenticator (auth + cache) y
│                 #   Web.config.
├── db/           # Esquema (db/init/) y utilidades SQL (seed, administración).
├── docker-compose.yml         # MySQL 8.4 (UTC) para desarrollo local.
└── tests/        # Pruebas de integración (xUnit + Testcontainers).
```

## Requisitos

- Visual Studio 2022 (o MSBuild) con el *targeting pack* de **.NET Framework 4.7.2**.
- **Docker** (para la base de datos de desarrollo y las pruebas de integración).
- El conector **MySqlConnector** se restaura por NuGet (`msbuild -t:restore`).

## Puesta en marcha

**1. Levantar la base de datos** (MySQL 8.4; crea el esquema y el usuario de
aplicación de mínimo privilegio automáticamente desde `db/init/`):

```bash
docker compose up -d
```

**2. (Opcional) Cargar datos de ejemplo** — crea la API key `local-dev.dev-secret`
y un par de notas:

```bash
docker exec -i notes-mysql mysql -uroot -proot_password notes < db/seed.sql
```

**3. Compilar y ejecutar**:

```bash
msbuild CRUD-WCF-ASP.Net-CSharp.sln -t:Restore
msbuild CRUD-WCF-ASP.Net-CSharp.sln -t:Build -p:Configuration=Debug
```

Desde Visual Studio, seleccioná `NoteService.svc` y ejecutá con **F5** para hostear
en IIS Express y abrir el **WCF Test Client** (el equivalente SOAP a una consola de
API; el contrato se publica como **WSDL**). Probá primero `Live`/`Health`.

> También podés apuntar el servicio a una base de datos MySQL ya existente (por
> ejemplo, una compartida con otros servicios): basta con que tenga el mismo
> esquema (`db/init/01-schema.sql`) y ajustar la cadena de conexión.

## Base de datos

- **`api_keys`**: `key_id`, `key_hash` (BINARY(32), SHA-256 del secreto),
  `client_name`, `created_at`, `revoked_at`, `expires_at`, `last_used_at`.
- **`notes`**: `id`, `owner_key_id` (FK a `api_keys`), `title` (VARCHAR 250),
  `text` (MEDIUMTEXT), `created_at`, `updated_at`. Índice FULLTEXT en `(title, text)`.

Notas de diseño:

- **Mínimo privilegio:** la aplicación se conecta como `notes_app` (solo
  `SELECT/INSERT/UPDATE/DELETE`, sin DDL; lo aprovisiona `db/init/02-app-user.sql`).
- **Fechas en UTC:** el servidor corre con `--default-time-zone=+00:00` y las
  columnas son `DATETIME`; la conversión a hora local es del consumidor.
- La administración de API keys (crear, revocar, detectar en desuso) está en
  [db/api-keys.sql](db/api-keys.sql).

## Configuración

| Clave (Web.config) | Descripción |
|--------------------|-------------|
| `connectionStrings/NotesDb` | Cadena de conexión a MySQL (usuario `notes_app`). |
| `RateLimiting:PermitLimit` | Peticiones permitidas por ventana e IP (default 100). |
| `RateLimiting:WindowSeconds` | Tamaño de la ventana en segundos (default 60). |
| `Authentication:KeyCacheSeconds` | TTL del cache en memoria de API keys (default 60). |
| `ForwardedHeaders:Enabled` | `true` solo detrás de un reverse proxy de confianza: usa `X-Forwarded-For` para el rate limiting. |

**Secretos en producción:** la cadena de conexión puede inyectarse por la variable
de entorno **`NOTES_DB_CONNECTION`** (tiene prioridad sobre el Web.config), para no
commitear credenciales. Alternativamente, se puede externalizar la sección
`connectionStrings` con `configSource="connections.config"` (ese archivo está en
`.gitignore`).

**Logging:** el servicio registra por el `TraceSource` **"Notes"** (nivel `Warning`
por defecto), configurable en `system.diagnostics` del Web.config; hay un listener
a archivo comentado como ejemplo.

## Autenticación

Todas las operaciones —salvo `Live`/`Health`— exigen una **API key** en el
parámetro `apiKey`, con formato `<key_id>.<secreto>` (p. ej. `local-dev.dev-secret`).
El servicio busca la key activa por `key_id` (no revocada ni expirada) y compara el
**SHA-256 del secreto en tiempo constante**. Las keys validadas se cachean en
memoria un TTL corto. **Cada cliente solo ve y modifica sus propias notas**; pedir
una nota ajena responde `NotFound`. Hay **rate limiting** por IP: al excederlo, la
respuesta trae `Status = RateLimited`.

## Operaciones

Como SOAP no tiene códigos de estado como HTTP, cada respuesta incluye un
`ResponseStatus` (`Ok`, `ValidationError`, `Unauthorized`, `NotFound`,
`RateLimited`, `Error`) además del mensaje.

| Operación | Parámetros | Devuelve | Notas |
|-----------|-----------|----------|-------|
| `Live` | — | `HealthResponse` | Liveness: el proceso responde (no toca la BD). |
| `Health` | — | `HealthResponse` | Readiness: la BD está alcanzable. |
| `List` | `apiKey, afterId, pageSize, search` | `NoteListResponse` | Paginación keyset + búsqueda. |
| `Get` | `apiKey, id` | `NoteResponse` | Nota con contenido completo. |
| `Add` | `apiKey, NoteRequest` | `NoteResponse` | Devuelve la nota creada. |
| `Update` | `apiKey, id, NoteRequest` | `Response` | |
| `Delete` | `apiKey, id` | `Response` | |

- **`List`** usa **paginación por keyset**: `pageSize` (1–100, default 20) y
  `afterId` (el `NextAfterId` de la página anterior; `null` = no hay más). Devuelve
  resúmenes (`NoteSummary`), sin el contenido. Con `search` filtra por **texto
  completo** (índice FULLTEXT; términos < 3 caracteres y *stopwords* no coinciden).
- **`NoteRequest`** lleva `Title` (obligatorio, máx. 250) y `Text` (opcional, máx.
  100.000 caracteres). El binding de WCF sube las cuotas
  (`maxReceivedMessageSize`/`readerQuotas`) para admitir esos tamaños.

## Pruebas

```bash
# Compilar y ejecutar las pruebas de integración (requiere Docker):
msbuild CRUD-WCF-ASP.Net-CSharp.sln -t:Restore
msbuild tests/Notes.IntegrationTests/Notes.IntegrationTests.csproj -p:Configuration=Debug
dotnet vstest tests/Notes.IntegrationTests/bin/Debug/net472/Notes.IntegrationTests.dll
```

Las pruebas levantan un **MySQL 8.4 efímero** con [Testcontainers](https://dotnet.testcontainers.org/),
le aplican el esquema real y ejercitan la capa de datos y la seguridad de API keys:
autenticación (hash/revocación/expiración), ciclo CRUD, aislamiento por cliente,
paginación keyset, búsqueda full-text y marcas de tiempo en UTC. En cada push,
GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) restaura,
compila, corre las pruebas y audita dependencias vulnerables.

## Limitaciones y decisiones (WCF/SOAP)

- SOAP no tiene códigos de estado HTTP → se expone un `ResponseStatus` por respuesta,
  y la documentación de la API es el **WSDL** (no OpenAPI/Scalar).
- El **rate limiting**, los **health checks** y el logging se resuelven en el propio
  servicio (no hay middleware como en ASP.NET Core). El rate limiter es en memoria y
  de mejor esfuerzo: con varias instancias, cada una lleva su propia cuenta.
- Las operaciones son síncronas (el acceso a datos con ADO.NET también lo es); no se
  usan `CancellationToken`.
