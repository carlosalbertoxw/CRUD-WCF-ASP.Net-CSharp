using System;
using System.Collections.Generic;
using Model;
using MySqlConnector;
using Utilities;

namespace Data
{
    /// <summary>
    /// Acceso a datos de las notas. Todas las operaciones están acotadas al dueño
    /// (<c>ownerKeyId</c>, el key_id autenticado): un cliente nunca ve ni modifica
    /// notas de otro. Las consultas son parametrizadas (sin concatenar valores).
    /// </summary>
    public class NoteDTO
    {
        private readonly DataAccess dataAccess;

        public NoteDTO()
        {
            dataAccess = new DataAccess();
        }

        /// <summary>Las fechas se leen como UTC (la base de datos corre en UTC).</summary>
        private static DateTime ReadUtc(MySqlDataReader reader, Int32 ordinal)
        {
            return DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc);
        }

        /// <summary>
        /// Página de notas del cliente con paginación por keyset (id &gt; afterId).
        /// Con <paramref name="search"/> se filtra por texto completo (índice
        /// FULLTEXT sobre título y contenido). Se pide un elemento extra para saber
        /// si hay página siguiente sin una consulta adicional.
        /// </summary>
        public NoteListResponse getPage(String ownerKeyId, Int32 afterId, Int32 pageSize, String search)
        {
            MySqlConnection connection = null;
            try
            {
                Boolean useSearch = !String.IsNullOrWhiteSpace(search);
                String filter = "owner_key_id = @ownerKeyId";
                if (useSearch)
                {
                    filter += " AND MATCH(title, text) AGAINST(@search IN NATURAL LANGUAGE MODE)";
                }

                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return null;
                }

                Int64 totalCount;
                using (MySqlCommand count = new MySqlCommand(
                    "SELECT COUNT(*) FROM notes WHERE " + filter + ";", connection))
                {
                    count.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    if (useSearch)
                    {
                        count.Parameters.AddWithValue("@search", search);
                    }
                    totalCount = Convert.ToInt64(count.ExecuteScalar());
                }

                List<NoteSummary> items = new List<NoteSummary>();
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT id, title, created_at, updated_at FROM notes WHERE " + filter +
                    " AND id > @afterId ORDER BY id LIMIT @limit;", connection))
                {
                    command.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    if (useSearch)
                    {
                        command.Parameters.AddWithValue("@search", search);
                    }
                    command.Parameters.AddWithValue("@afterId", afterId);
                    command.Parameters.AddWithValue("@limit", pageSize + 1);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new NoteSummary
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                CreatedAt = ReadUtc(reader, 2),
                                UpdatedAt = ReadUtc(reader, 3)
                            });
                        }
                    }
                }

                Boolean hasMore = items.Count > pageSize;
                if (hasMore)
                {
                    items.RemoveAt(items.Count - 1);
                }

                return new NoteListResponse
                {
                    Items = items,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    NextAfterId = hasMore ? (Int32?)items[items.Count - 1].Id : null
                };
            }
            catch (Exception ex)
            {
                Log.Error("Error de acceso a datos en NoteDTO.", ex);
                return null;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }

        /// <summary>Obtiene una nota del cliente por id, o null si no existe/ no es suya.</summary>
        public Note get(String ownerKeyId, Int32 id)
        {
            MySqlConnection connection = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return null;
                }
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT id, title, text, created_at, updated_at FROM notes " +
                    "WHERE id = @id AND owner_key_id = @ownerKeyId;", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }
                        return new Note
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Text = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CreatedAt = ReadUtc(reader, 3),
                            UpdatedAt = ReadUtc(reader, 4)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error de acceso a datos en NoteDTO.", ex);
                return null;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }

        /// <summary>
        /// Crea una nota para el cliente y devuelve la fila resultante (con id y las
        /// marcas de tiempo generadas por la base de datos), o null si falla.
        /// </summary>
        public Note add(String ownerKeyId, String title, String text)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return null;
                }
                transaction = connection.BeginTransaction();

                Int64 newId;
                using (MySqlCommand command = new MySqlCommand(
                    "INSERT INTO notes(owner_key_id, title, text) VALUES(@ownerKeyId, @title, @text);",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@text", (Object)text ?? DBNull.Value);
                    command.ExecuteNonQuery();
                    newId = command.LastInsertedId;
                }

                Note created = null;
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT id, title, text, created_at, updated_at FROM notes WHERE id = @id;",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", newId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            created = new Note
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Text = reader.IsDBNull(2) ? null : reader.GetString(2),
                                CreatedAt = ReadUtc(reader, 3),
                                UpdatedAt = ReadUtc(reader, 4)
                            };
                        }
                    }
                }

                transaction.Commit();
                return created;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
                Log.Error("Error al crear la nota (transacción revertida).", ex);
                return null;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }

        /// <summary>Actualiza una nota del cliente. Devuelve false si no existe/ no es suya.</summary>
        public Boolean update(String ownerKeyId, Int32 id, String title, String text)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return false;
                }
                transaction = connection.BeginTransaction();

                Int32 rows;
                using (MySqlCommand command = new MySqlCommand(
                    "UPDATE notes SET title = @title, text = @text " +
                    "WHERE id = @id AND owner_key_id = @ownerKeyId;", connection, transaction))
                {
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@text", (Object)text ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    rows = command.ExecuteNonQuery();
                }

                transaction.Commit();
                return rows > 0;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
                Log.Error("Error de acceso a datos en NoteDTO (transacción revertida).", ex);
                return false;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }

        /// <summary>Elimina una nota del cliente. Devuelve false si no existe/ no es suya.</summary>
        public Boolean delete(String ownerKeyId, Int32 id)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                connection = dataAccess.openConnection();
                if (connection == null)
                {
                    return false;
                }
                transaction = connection.BeginTransaction();

                Int32 rows;
                using (MySqlCommand command = new MySqlCommand(
                    "DELETE FROM notes WHERE id = @id AND owner_key_id = @ownerKeyId;",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@ownerKeyId", ownerKeyId);
                    rows = command.ExecuteNonQuery();
                }

                transaction.Commit();
                return rows > 0;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try { transaction.Rollback(); } catch { }
                }
                Log.Error("Error de acceso a datos en NoteDTO (transacción revertida).", ex);
                return false;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }
        }
    }
}
