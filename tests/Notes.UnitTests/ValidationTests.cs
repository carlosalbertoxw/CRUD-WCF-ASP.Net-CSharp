using System;
using Utilities;
using Xunit;

namespace Notes.UnitTests
{
    /// <summary>
    /// Pruebas de la validación de notas. Los límites reflejan los de la base de
    /// datos: título VARCHAR(250) y contenido MEDIUMTEXT acotado a 100.000.
    /// </summary>
    public class ValidationTests
    {
        [Fact]
        public void Valid_note_returns_no_error()
        {
            Assert.Null(Validation.ValidateNote("Lista compras", "zanahoria y lechuga"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \t ")]
        public void Title_is_required(string title)
        {
            Assert.Equal(Message.ERROR_TITLE_REQUIRED, Validation.ValidateNote(title, "x"));
        }

        [Fact]
        public void Title_of_exactly_the_maximum_length_is_accepted()
        {
            string title = new string('a', Validation.TITLE_MAX_LENGTH);

            Assert.Null(Validation.ValidateNote(title, "x"));
        }

        [Fact]
        public void Title_one_character_over_the_maximum_is_rejected()
        {
            string title = new string('a', Validation.TITLE_MAX_LENGTH + 1);

            Assert.Equal(Message.ERROR_TITLE_TOO_LONG, Validation.ValidateNote(title, "x"));
        }

        [Fact]
        public void Text_is_optional()
        {
            Assert.Null(Validation.ValidateNote("Titulo", null));
        }

        [Fact]
        public void Text_of_exactly_the_maximum_length_is_accepted()
        {
            string text = new string('a', Validation.TEXT_MAX_LENGTH);

            Assert.Null(Validation.ValidateNote("Titulo", text));
        }

        [Fact]
        public void Text_one_character_over_the_maximum_is_rejected()
        {
            string text = new string('a', Validation.TEXT_MAX_LENGTH + 1);

            Assert.Equal(Message.ERROR_TEXT_TOO_LONG, Validation.ValidateNote("Titulo", text));
        }

        [Fact]
        public void The_first_error_wins_when_several_rules_fail()
        {
            // Título vacío y contenido excedido a la vez: se reporta el del título.
            string text = new string('a', Validation.TEXT_MAX_LENGTH + 1);

            Assert.Equal(Message.ERROR_TITLE_REQUIRED, Validation.ValidateNote("", text));
        }
    }
}
