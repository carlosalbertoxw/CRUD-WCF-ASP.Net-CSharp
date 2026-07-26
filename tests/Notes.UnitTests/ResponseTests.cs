using System;
using Model;
using Xunit;

namespace Notes.UnitTests
{
    /// <summary>
    /// Pruebas de la respuesta base del servicio. <c>Success</c> no es un campo
    /// independiente: se deriva de <c>Status</c>, que es el equivalente SOAP a los
    /// códigos de estado HTTP de la versión REST.
    /// </summary>
    public class ResponseTests
    {
        [Fact]
        public void Success_is_true_only_when_the_status_is_ok()
        {
            Assert.True(new Response { Status = ResponseStatus.Ok }.Success);
        }

        [Theory]
        [InlineData(ResponseStatus.ValidationError)]
        [InlineData(ResponseStatus.Unauthorized)]
        [InlineData(ResponseStatus.NotFound)]
        [InlineData(ResponseStatus.RateLimited)]
        [InlineData(ResponseStatus.Error)]
        public void Success_is_false_for_every_error_status(ResponseStatus status)
        {
            Assert.False(new Response { Status = status }.Success);
        }

        [Fact]
        public void Assigning_success_does_not_change_the_state()
        {
            // El setter existe solo para que el serializador de contratos de datos
            // pueda escribir la propiedad; no debe poder falsear el resultado.
            Response response = new Response { Status = ResponseStatus.NotFound };

            response.Success = true;

            Assert.False(response.Success);
            Assert.Equal(ResponseStatus.NotFound, response.Status);
        }

        [Fact]
        public void Derived_responses_inherit_the_same_rule()
        {
            Assert.True(new NoteResponse { Status = ResponseStatus.Ok }.Success);
            Assert.False(new NoteResponse { Status = ResponseStatus.Error }.Success);

            Assert.True(new NoteListResponse { Status = ResponseStatus.Ok }.Success);
            Assert.False(new NoteListResponse { Status = ResponseStatus.Error }.Success);

            Assert.True(new HealthResponse { Status = ResponseStatus.Ok }.Success);
            Assert.False(new HealthResponse { Status = ResponseStatus.Error }.Success);
        }
    }
}
