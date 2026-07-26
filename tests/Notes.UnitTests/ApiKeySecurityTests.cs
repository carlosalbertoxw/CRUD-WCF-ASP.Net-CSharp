using System;
using Utilities;
using Xunit;

namespace Notes.UnitTests
{
    /// <summary>
    /// Pruebas de la criptografía de API keys: separación de "&lt;key_id&gt;.&lt;secreto&gt;",
    /// hash SHA-256 del secreto y comparación en tiempo constante. Todo es lógica
    /// pura, sin base de datos.
    /// </summary>
    public class ApiKeySecurityTests
    {
        private static string Hex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        // ------------------------------------------------------------------ TryParse

        [Fact]
        public void TryParse_splits_key_id_and_secret()
        {
            ApiKeyParts parts;

            Assert.True(ApiKeySecurity.TryParse("kid-01.s3cret-largo", out parts));
            Assert.Equal("kid-01", parts.KeyId);
            Assert.Equal("s3cret-largo", parts.Secret);
        }

        [Fact]
        public void TryParse_splits_on_the_first_dot_so_the_secret_may_contain_dots()
        {
            ApiKeyParts parts;

            Assert.True(ApiKeySecurity.TryParse("kid.a.b.c", out parts));
            Assert.Equal("kid", parts.KeyId);
            Assert.Equal("a.b.c", parts.Secret);
        }

        [Theory]
        [InlineData(null)]                  // sin valor
        [InlineData("")]                    // cadena vacía
        [InlineData("kidsecretosinpunto")]  // sin separador
        [InlineData(".s3cret")]             // key_id vacío
        [InlineData("kid.")]                // secreto vacío
        public void TryParse_rejects_malformed_keys(string apiKey)
        {
            ApiKeyParts parts;

            Assert.False(ApiKeySecurity.TryParse(apiKey, out parts));
        }

        [Fact]
        public void TryParse_leaves_the_out_parameter_empty_when_it_fails()
        {
            ApiKeyParts parts;

            Assert.False(ApiKeySecurity.TryParse("sinpunto", out parts));
            Assert.Null(parts.KeyId);
            Assert.Null(parts.Secret);
        }

        // ---------------------------------------------------------------- HashSecret

        [Fact]
        public void HashSecret_matches_the_known_sha256_vector()
        {
            Assert.Equal(
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                Hex(ApiKeySecurity.HashSecret("abc")));
        }

        [Theory]
        [InlineData("")]
        [InlineData("x")]
        [InlineData("un secreto bastante mas largo que el tamano del bloque de sha-256")]
        public void HashSecret_always_returns_32_bytes(string secret)
        {
            // La tabla api_keys guarda el hash en un BINARY(32).
            Assert.Equal(32, ApiKeySecurity.HashSecret(secret).Length);
        }

        [Fact]
        public void HashSecret_treats_null_as_the_empty_string()
        {
            Assert.Equal(
                "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                Hex(ApiKeySecurity.HashSecret(null)));
            Assert.Equal(Hex(ApiKeySecurity.HashSecret(string.Empty)),
                         Hex(ApiKeySecurity.HashSecret(null)));
        }

        [Fact]
        public void HashSecret_is_deterministic_and_utf8_sensitive()
        {
            Assert.Equal(Hex(ApiKeySecurity.HashSecret("café")),
                         Hex(ApiKeySecurity.HashSecret("café")));
            Assert.NotEqual(Hex(ApiKeySecurity.HashSecret("café")),
                            Hex(ApiKeySecurity.HashSecret("cafe")));
        }

        // ------------------------------------------------------------- SecretMatches

        [Fact]
        public void SecretMatches_accepts_the_right_secret()
        {
            byte[] stored = ApiKeySecurity.HashSecret("s3cret");

            Assert.True(ApiKeySecurity.SecretMatches("s3cret", stored));
        }

        [Fact]
        public void SecretMatches_rejects_a_wrong_secret()
        {
            byte[] stored = ApiKeySecurity.HashSecret("s3cret");

            Assert.False(ApiKeySecurity.SecretMatches("otro", stored));
        }

        [Fact]
        public void SecretMatches_rejects_a_null_stored_hash()
        {
            // Es el caso de una key que no existe en la base de datos.
            Assert.False(ApiKeySecurity.SecretMatches("s3cret", null));
        }

        [Fact]
        public void SecretMatches_rejects_a_stored_hash_of_the_wrong_length()
        {
            Assert.False(ApiKeySecurity.SecretMatches("s3cret", new byte[] { 1, 2, 3 }));
        }

        // ----------------------------------------------------------- FixedTimeEquals

        [Fact]
        public void FixedTimeEquals_is_true_for_equal_contents()
        {
            Assert.True(ApiKeySecurity.FixedTimeEquals(
                new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }));
        }

        [Theory]
        [InlineData(new byte[] { 9, 2, 3 })]  // difiere el primero
        [InlineData(new byte[] { 1, 9, 3 })]  // difiere el de en medio
        [InlineData(new byte[] { 1, 2, 9 })]  // difiere el último
        public void FixedTimeEquals_detects_a_single_differing_byte(byte[] other)
        {
            Assert.False(ApiKeySecurity.FixedTimeEquals(new byte[] { 1, 2, 3 }, other));
        }

        [Fact]
        public void FixedTimeEquals_is_false_for_different_lengths()
        {
            Assert.False(ApiKeySecurity.FixedTimeEquals(
                new byte[] { 1, 2 }, new byte[] { 1, 2, 3 }));
        }

        [Fact]
        public void FixedTimeEquals_is_false_when_either_side_is_null()
        {
            byte[] value = { 1, 2, 3 };

            Assert.False(ApiKeySecurity.FixedTimeEquals(null, value));
            Assert.False(ApiKeySecurity.FixedTimeEquals(value, null));
            Assert.False(ApiKeySecurity.FixedTimeEquals(null, null));
        }

        [Fact]
        public void FixedTimeEquals_is_true_for_two_empty_arrays()
        {
            Assert.True(ApiKeySecurity.FixedTimeEquals(new byte[0], new byte[0]));
        }
    }
}
