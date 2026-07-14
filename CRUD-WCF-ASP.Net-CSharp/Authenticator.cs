using System;
using System.Collections.Concurrent;
using System.Configuration;
using Data;
using Model;
using Utilities;

namespace CRUD_WCF_ASP.Net_CSharp
{
    /// <summary>Resultado de autenticar una API key.</summary>
    public class AuthResult
    {
        /// <summary>La key es válida, activa y el secreto coincide.</summary>
        public Boolean Authenticated { get; set; }
        /// <summary>El formato de la API key no era "&lt;key_id&gt;.&lt;secreto&gt;".</summary>
        public Boolean FormatError { get; set; }
        /// <summary>key_id autenticado; acota todas las operaciones a sus notas.</summary>
        public String KeyId { get; set; }
        public String ClientName { get; set; }
    }

    /// <summary>
    /// Autenticación por API key: separa "&lt;key_id&gt;.&lt;secreto&gt;", busca la key
    /// activa y compara el SHA-256 del secreto en tiempo constante. Las keys
    /// validadas se cachean en memoria un TTL corto (Authentication:KeyCacheSeconds,
    /// 60 s por defecto): el caso caliente autentica sin tocar la base de datos. El
    /// costo asumido es que una revocación tarda hasta ese TTL en surtir efecto.
    /// El estado es estático porque WCF instancia el servicio por llamada.
    /// </summary>
    public static class Authenticator
    {
        private class KeyCacheEntry
        {
            public ApiKey Key;          // puede ser null: también se cachea "no existe".
            public DateTime Expiry;     // UTC.
        }

        private static readonly ApiKeyDTO apiKeyDTO = new ApiKeyDTO();
        private static readonly ConcurrentDictionary<String, KeyCacheEntry> keyCache =
            new ConcurrentDictionary<String, KeyCacheEntry>();
        private static readonly ConcurrentDictionary<String, DateTime> usedCache =
            new ConcurrentDictionary<String, DateTime>();

        private static readonly TimeSpan KeyTtl = TimeSpan.FromSeconds(
            ReadInt("Authentication:KeyCacheSeconds", 60));
        // Menor que el INTERVAL 1 HOUR del UPDATE de last_used_at.
        private static readonly TimeSpan UsedTtl = TimeSpan.FromMinutes(50);

        public static AuthResult Authenticate(String apiKey)
        {
            ApiKeyParts parts;
            if (!ApiKeySecurity.TryParse(apiKey, out parts))
            {
                return new AuthResult { Authenticated = false, FormatError = true };
            }

            ApiKey key = GetActiveKeyCached(parts.KeyId);
            if (key == null || !ApiKeySecurity.SecretMatches(parts.Secret, key.KeyHash))
            {
                return new AuthResult { Authenticated = false };
            }

            RegisterUseDeduped(key.KeyId);
            return new AuthResult
            {
                Authenticated = true,
                KeyId = key.KeyId,
                ClientName = key.ClientName
            };
        }

        private static ApiKey GetActiveKeyCached(String keyId)
        {
            DateTime now = DateTime.UtcNow;
            KeyCacheEntry entry;
            if (keyCache.TryGetValue(keyId, out entry) && entry.Expiry > now)
            {
                return entry.Key;
            }

            ApiKey key = apiKeyDTO.getActiveKey(keyId);
            keyCache[keyId] = new KeyCacheEntry { Key = key, Expiry = now.Add(KeyTtl) };
            return key;
        }

        private static void RegisterUseDeduped(String keyId)
        {
            DateTime now = DateTime.UtcNow;
            DateTime until;
            if (usedCache.TryGetValue(keyId, out until) && until > now)
            {
                return;
            }
            usedCache[keyId] = now.Add(UsedTtl);
            apiKeyDTO.registerUse(keyId);
        }

        private static Int32 ReadInt(String key, Int32 fallback)
        {
            Int32 value;
            String raw = ConfigurationManager.AppSettings[key];
            return Int32.TryParse(raw, out value) && value > 0 ? value : fallback;
        }
    }
}
