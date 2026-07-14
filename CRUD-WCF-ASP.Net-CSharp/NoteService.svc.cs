using System;
using System.Configuration;
using System.ServiceModel;
using Data;
using Model;
using Utilities;

namespace CRUD_WCF_ASP.Net_CSharp
{
    /// <summary>
    /// Implementación del servicio de notas. Cada operación (salvo Health) aplica,
    /// en orden: rate limiting por IP, autenticación por API key y validación de
    /// entrada; luego delega en la capa de datos acotando todo al cliente dueño.
    /// El resultado se expresa con un <see cref="ResponseStatus"/> (el equivalente
    /// SOAP a los códigos HTTP de la versión REST).
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly NoteDTO noteDTO;

        // Compartido entre llamadas (WCF instancia el servicio por petición).
        private static readonly RateLimiter rateLimiter = new RateLimiter(
            ReadInt("RateLimiting:PermitLimit", 100),
            ReadInt("RateLimiting:WindowSeconds", 60));

        // Solo se confía en X-Forwarded-For si hay un reverse proxy de confianza
        // delante; por defecto está desactivado.
        private static readonly Boolean forwardedHeadersEnabled =
            ReadBool("ForwardedHeaders:Enabled", false);

        public NoteService()
        {
            noteDTO = new NoteDTO();
        }

        // ------------------------------------------------------------------- Live

        public HealthResponse Live()
        {
            // Liveness: si esta línea se ejecuta, el proceso está vivo. No toca la BD.
            return new HealthResponse
            {
                DatabaseReachable = false,
                Status = ResponseStatus.Ok,
                Message = Message.HEALTH_OK
            };
        }

        // ----------------------------------------------------------------- Health

        public HealthResponse Health()
        {
            HealthResponse response = new HealthResponse();
            try
            {
                Boolean reachable = new DataAccess().canConnect();
                response.DatabaseReachable = reachable;
                response.Status = reachable ? ResponseStatus.Ok : ResponseStatus.Error;
                response.Message = reachable ? Message.HEALTH_OK : Message.HEALTH_DB_DOWN;
            }
            catch (Exception ex)
            {
                LogError(ex);
                response.DatabaseReachable = false;
                response.Status = ResponseStatus.Error;
                response.Message = Message.HEALTH_DB_DOWN;
            }
            return response;
        }

        // -------------------------------------------------------------------- List

        public NoteListResponse List(String apiKey, Int32 afterId, Int32 pageSize, String search)
        {
            NoteListResponse response = new NoteListResponse();
            try
            {
                String ownerKeyId;
                if (!Guard(apiKey, response, out ownerKeyId))
                {
                    return response;
                }

                // Se acotan los parámetros de paginación a rangos sanos.
                if (afterId < 0) afterId = 0;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                NoteListResponse page = noteDTO.getPage(ownerKeyId, afterId, pageSize, search);
                if (page == null)
                {
                    return Fail(response, ResponseStatus.Error, Message.ERROR_CONSULT);
                }

                response.Items = page.Items;
                response.PageSize = page.PageSize;
                response.TotalCount = page.TotalCount;
                response.NextAfterId = page.NextAfterId;
                response.Status = ResponseStatus.Ok;
                response.Message = Message.SUCCESSFUL_CONSULT;
            }
            catch (Exception ex)
            {
                LogError(ex);
                Fail(response, ResponseStatus.Error, Message.ERROR_PROCESSING_DATA);
            }
            return response;
        }

        // --------------------------------------------------------------------- Get

        public NoteResponse Get(String apiKey, Int32 id)
        {
            NoteResponse response = new NoteResponse();
            try
            {
                String ownerKeyId;
                if (!Guard(apiKey, response, out ownerKeyId))
                {
                    return response;
                }

                Note note = noteDTO.get(ownerKeyId, id);
                if (note == null)
                {
                    return Fail(response, ResponseStatus.NotFound, Message.ERROR_NOTE_NOT_FOUND);
                }

                response.Note = note;
                response.Status = ResponseStatus.Ok;
                response.Message = Message.SUCCESSFUL_CONSULT;
            }
            catch (Exception ex)
            {
                LogError(ex);
                Fail(response, ResponseStatus.Error, Message.ERROR_PROCESSING_DATA);
            }
            return response;
        }

        // --------------------------------------------------------------------- Add

        public NoteResponse Add(String apiKey, NoteRequest note)
        {
            NoteResponse response = new NoteResponse();
            try
            {
                String ownerKeyId;
                if (!Guard(apiKey, response, out ownerKeyId))
                {
                    return response;
                }
                if (note == null)
                {
                    return Fail(response, ResponseStatus.ValidationError, Message.ERROR_NOTE_REQUIRED);
                }
                String validation = Validation.ValidateNote(note.Title, note.Text);
                if (validation != null)
                {
                    return Fail(response, ResponseStatus.ValidationError, validation);
                }

                Note created = noteDTO.add(ownerKeyId, note.Title, note.Text);
                if (created == null)
                {
                    return Fail(response, ResponseStatus.Error, Message.ERROR_SAVE);
                }

                response.Note = created;
                response.Status = ResponseStatus.Ok;
                response.Message = Message.SUCCESSFUL_SAVE;
            }
            catch (Exception ex)
            {
                LogError(ex);
                Fail(response, ResponseStatus.Error, Message.ERROR_PROCESSING_DATA);
            }
            return response;
        }

        // ------------------------------------------------------------------ Update

        public Response Update(String apiKey, Int32 id, NoteRequest note)
        {
            Response response = new Response();
            try
            {
                String ownerKeyId;
                if (!Guard(apiKey, response, out ownerKeyId))
                {
                    return response;
                }
                if (note == null)
                {
                    return Fail(response, ResponseStatus.ValidationError, Message.ERROR_NOTE_REQUIRED);
                }
                String validation = Validation.ValidateNote(note.Title, note.Text);
                if (validation != null)
                {
                    return Fail(response, ResponseStatus.ValidationError, validation);
                }

                if (!noteDTO.update(ownerKeyId, id, note.Title, note.Text))
                {
                    return Fail(response, ResponseStatus.NotFound, Message.ERROR_NOTE_NOT_FOUND);
                }

                response.Status = ResponseStatus.Ok;
                response.Message = Message.SUCCESSFUL_UPDATE;
            }
            catch (Exception ex)
            {
                LogError(ex);
                Fail(response, ResponseStatus.Error, Message.ERROR_PROCESSING_DATA);
            }
            return response;
        }

        // ------------------------------------------------------------------ Delete

        public Response Delete(String apiKey, Int32 id)
        {
            Response response = new Response();
            try
            {
                String ownerKeyId;
                if (!Guard(apiKey, response, out ownerKeyId))
                {
                    return response;
                }

                if (!noteDTO.delete(ownerKeyId, id))
                {
                    return Fail(response, ResponseStatus.NotFound, Message.ERROR_NOTE_NOT_FOUND);
                }

                response.Status = ResponseStatus.Ok;
                response.Message = Message.SUCCESSFUL_DELETE;
            }
            catch (Exception ex)
            {
                LogError(ex);
                Fail(response, ResponseStatus.Error, Message.ERROR_PROCESSING_DATA);
            }
            return response;
        }

        // ----------------------------------------------------------------- Helpers

        /// <summary>
        /// Guarda común a todas las operaciones autenticadas: aplica rate limiting
        /// por IP y luego autentica la API key. Si algo falla, rellena
        /// <paramref name="response"/> con el estado adecuado y devuelve false.
        /// </summary>
        private static Boolean Guard(String apiKey, Response response, out String ownerKeyId)
        {
            ownerKeyId = null;

            if (!rateLimiter.IsAllowed(GetClientIp()))
            {
                Fail(response, ResponseStatus.RateLimited, Message.ERROR_RATE_LIMITED);
                return false;
            }

            AuthResult auth = Authenticator.Authenticate(apiKey);
            if (!auth.Authenticated)
            {
                Fail(response, ResponseStatus.Unauthorized,
                    auth.FormatError ? Message.ERROR_AUTH_FORMAT : Message.ERROR_SESSION_VALIDATION);
                return false;
            }

            ownerKeyId = auth.KeyId;
            return true;
        }

        /// <summary>Rellena una respuesta con estado de error y devuelve la misma instancia.</summary>
        private static T Fail<T>(T response, ResponseStatus status, String message) where T : Response
        {
            response.Status = status;
            response.Message = message;
            return response;
        }

        /// <summary>
        /// IP del cliente para el rate limiting. Si <c>ForwardedHeaders:Enabled</c>
        /// está activo (despliegue detrás de un reverse proxy de confianza que
        /// termina TLS), se toma el primer salto de <c>X-Forwarded-For</c>; de lo
        /// contrario se usa la IP del socket. Por defecto está desactivado
        /// (fail-safe): confiar en el encabezado sin un proxy delante permitiría a
        /// cualquiera falsear su IP y evadir el límite.
        /// </summary>
        private static String GetClientIp()
        {
            try
            {
                OperationContext context = OperationContext.Current;
                if (context == null)
                {
                    return "unknown";
                }
                System.ServiceModel.Channels.MessageProperties props = context.IncomingMessageProperties;

                if (forwardedHeadersEnabled &&
                    props.ContainsKey(System.ServiceModel.Channels.HttpRequestMessageProperty.Name))
                {
                    System.ServiceModel.Channels.HttpRequestMessageProperty http =
                        (System.ServiceModel.Channels.HttpRequestMessageProperty)
                            props[System.ServiceModel.Channels.HttpRequestMessageProperty.Name];
                    String forwarded = http.Headers["X-Forwarded-For"];
                    if (!String.IsNullOrWhiteSpace(forwarded))
                    {
                        String first = forwarded.Split(',')[0].Trim();
                        if (first.Length > 0)
                        {
                            return first;
                        }
                    }
                }

                if (props.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
                {
                    System.ServiceModel.Channels.RemoteEndpointMessageProperty endpoint =
                        (System.ServiceModel.Channels.RemoteEndpointMessageProperty)
                            props[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name];
                    return endpoint.Address;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("No se pudo obtener la IP del cliente.", ex);
            }
            return "unknown";
        }

        private static void LogError(Exception ex)
        {
            Log.Error("Error no controlado en el servicio de notas.", ex);
        }

        private static Int32 ReadInt(String key, Int32 fallback)
        {
            Int32 value;
            String raw = ConfigurationManager.AppSettings[key];
            return Int32.TryParse(raw, out value) && value > 0 ? value : fallback;
        }

        private static Boolean ReadBool(String key, Boolean fallback)
        {
            Boolean value;
            String raw = ConfigurationManager.AppSettings[key];
            return Boolean.TryParse(raw, out value) ? value : fallback;
        }
    }
}
