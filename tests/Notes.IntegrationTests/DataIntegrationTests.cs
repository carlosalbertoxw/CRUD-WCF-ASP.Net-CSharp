using System;
using Data;
using Model;
using Utilities;
using Xunit;

namespace Notes.IntegrationTests
{
    /// <summary>
    /// Pruebas de integración de la capa de datos y la seguridad de API keys contra
    /// un MySQL 8.4 real (Testcontainers). Cubren autenticación (hash/revocación/
    /// expiración), ciclo CRUD, aislamiento por cliente, paginación por keyset,
    /// búsqueda de texto completo y marcas de tiempo en UTC.
    /// </summary>
    [Collection("mysql")]
    public class DataIntegrationTests
    {
        private readonly MySqlFixture fixture;
        private readonly NoteDTO notes = new NoteDTO();
        private readonly ApiKeyDTO apiKeys = new ApiKeyDTO();

        public DataIntegrationTests(MySqlFixture fixture)
        {
            this.fixture = fixture;
        }

        /// <summary>Crea una API key única (para aislar cada prueba) y devuelve su id.</summary>
        private string NewKey(string secret, DateTime? expiresAt = null, DateTime? revokedAt = null)
        {
            string keyId = "k-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            fixture.InsertApiKey(keyId, secret, expiresAt, revokedAt);
            return keyId;
        }

        // ------------------------------------------------------------ Autenticación

        [Fact]
        public void Active_key_is_found_and_secret_matches()
        {
            string id = NewKey("s3cret-largo");

            ApiKey key = apiKeys.getActiveKey(id);

            Assert.NotNull(key);
            Assert.Equal(id, key.KeyId);
            Assert.True(ApiKeySecurity.SecretMatches("s3cret-largo", key.KeyHash));
            Assert.False(ApiKeySecurity.SecretMatches("secreto-incorrecto", key.KeyHash));
        }

        [Fact]
        public void Revoked_key_is_not_returned()
        {
            string id = NewKey("x", revokedAt: DateTime.UtcNow.AddMinutes(-1));
            Assert.Null(apiKeys.getActiveKey(id));
        }

        [Fact]
        public void Expired_key_is_not_returned()
        {
            string id = NewKey("x", expiresAt: DateTime.UtcNow.AddMinutes(-1));
            Assert.Null(apiKeys.getActiveKey(id));
        }

        // -------------------------------------------------------------------- CRUD

        [Fact]
        public void Add_then_get_roundtrips_with_utc_timestamps()
        {
            string owner = NewKey("s");

            Note created = notes.add(owner, "Título", "Contenido de la nota");

            Assert.NotNull(created);
            Assert.True(created.Id > 0);
            Assert.Equal("Título", created.Title);
            Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, created.UpdatedAt.Kind);

            Note fetched = notes.get(owner, created.Id);
            Assert.NotNull(fetched);
            Assert.Equal("Contenido de la nota", fetched.Text);
        }

        [Fact]
        public void Notes_are_isolated_by_owner()
        {
            string a = NewKey("s");
            string b = NewKey("s");
            Note ofA = notes.add(a, "Privada de A", "solo A");

            Assert.Null(notes.get(b, ofA.Id));

            NoteListResponse pageOfB = notes.getPage(b, 0, 20, null);
            Assert.Empty(pageOfB.Items);
            Assert.Equal(0, pageOfB.TotalCount);
        }

        [Fact]
        public void Update_and_delete_are_scoped_to_owner()
        {
            string a = NewKey("s");
            string b = NewKey("s");
            Note note = notes.add(a, "original", "x");

            // Otro cliente no puede tocarla.
            Assert.False(notes.update(b, note.Id, "hackeado", "h"));
            Assert.False(notes.delete(b, note.Id));

            // El dueño sí.
            Assert.True(notes.update(a, note.Id, "actualizado", "y"));
            Assert.Equal("actualizado", notes.get(a, note.Id).Title);
            Assert.True(notes.delete(a, note.Id));
            Assert.Null(notes.get(a, note.Id));
        }

        // -------------------------------------------------------------- Paginación

        [Fact]
        public void Keyset_pagination_walks_all_pages_once()
        {
            string owner = NewKey("s");
            for (int i = 1; i <= 5; i++)
            {
                notes.add(owner, "nota " + i, "cuerpo " + i);
            }

            NoteListResponse p1 = notes.getPage(owner, 0, 2, null);
            Assert.Equal(2, p1.Items.Count);
            Assert.Equal(5, p1.TotalCount);
            Assert.NotNull(p1.NextAfterId);

            NoteListResponse p2 = notes.getPage(owner, p1.NextAfterId.Value, 2, null);
            Assert.Equal(2, p2.Items.Count);
            Assert.NotNull(p2.NextAfterId);

            NoteListResponse p3 = notes.getPage(owner, p2.NextAfterId.Value, 2, null);
            Assert.Single(p3.Items);
            Assert.Null(p3.NextAfterId);

            // Ids estrictamente crecientes y sin repetición entre páginas.
            Assert.True(p1.Items[0].Id < p1.Items[1].Id);
            Assert.True(p1.Items[1].Id < p2.Items[0].Id);
            Assert.True(p2.Items[1].Id < p3.Items[0].Id);
        }

        // ---------------------------------------------------------------- Búsqueda

        [Fact]
        public void Fulltext_search_filters_by_title_and_text()
        {
            string owner = NewKey("s");
            notes.add(owner, "Lista compras", "zanahoria y lechuga");
            notes.add(owner, "Reunion equipo", "agenda del lunes");

            NoteListResponse byText = notes.getPage(owner, 0, 20, "zanahoria");
            Assert.Single(byText.Items);
            Assert.Equal("Lista compras", byText.Items[0].Title);

            NoteListResponse byTitle = notes.getPage(owner, 0, 20, "Reunion");
            Assert.Single(byTitle.Items);
            Assert.Equal("Reunion equipo", byTitle.Items[0].Title);
        }
    }
}
