using System;
using System.Security.Cryptography;
using System.Text;

namespace Utilities
{
    /// <summary>
    /// El cliente presenta la API key con el formato <c>&lt;key_id&gt;.&lt;secreto&gt;</c>.
    /// </summary>
    public struct ApiKeyParts
    {
        public String KeyId;
        public String Secret;
    }

    /// <summary>
    /// Utilidades criptográficas para las API keys: separar id y secreto, hashear
    /// el secreto (SHA-256) y compararlo en tiempo constante contra el hash
    /// almacenado. El secreto nunca se guarda ni se registra en claro.
    /// </summary>
    public static class ApiKeySecurity
    {
        /// <summary>
        /// Separa "&lt;key_id&gt;.&lt;secreto&gt;" en sus dos partes. Devuelve false si el
        /// formato no es válido (falta el punto o alguna parte está vacía).
        /// </summary>
        public static Boolean TryParse(String apiKey, out ApiKeyParts parts)
        {
            parts = new ApiKeyParts();
            if (String.IsNullOrEmpty(apiKey))
            {
                return false;
            }

            Int32 separator = apiKey.IndexOf('.');
            if (separator <= 0 || separator >= apiKey.Length - 1)
            {
                return false;
            }

            parts.KeyId = apiKey.Substring(0, separator);
            parts.Secret = apiKey.Substring(separator + 1);
            return true;
        }

        /// <summary>Devuelve el SHA-256 (32 bytes) del secreto codificado en UTF-8.</summary>
        public static Byte[] HashSecret(String secret)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(secret ?? String.Empty));
            }
        }

        /// <summary>
        /// Compara el hash del secreto presentado contra el almacenado en tiempo
        /// constante, para no filtrar información por el tiempo de respuesta.
        /// </summary>
        public static Boolean SecretMatches(String providedSecret, Byte[] storedHash)
        {
            if (storedHash == null)
            {
                return false;
            }

            Byte[] providedHash = HashSecret(providedSecret);
            return FixedTimeEquals(providedHash, storedHash);
        }

        /// <summary>
        /// Comparación de arreglos de bytes en tiempo constante. Equivale a
        /// <c>CryptographicOperations.FixedTimeEquals</c>, no disponible en .NET
        /// Framework.
        /// </summary>
        public static Boolean FixedTimeEquals(Byte[] a, Byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            Int32 diff = 0;
            for (Int32 i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
