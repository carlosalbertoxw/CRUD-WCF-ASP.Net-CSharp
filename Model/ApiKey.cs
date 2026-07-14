using System;

namespace Model
{
    /// <summary>
    /// Registro de una API key en la tabla <c>api_keys</c>. El secreto nunca se
    /// guarda ni viaja en claro: solo se conserva su hash SHA-256 (BINARY(32)).
    /// No se expone como contrato de datos; es de uso interno del servicio.
    /// </summary>
    public class ApiKey
    {
        public String KeyId { get; set; }
        public Byte[] KeyHash { get; set; }
        public String ClientName { get; set; }
    }
}
