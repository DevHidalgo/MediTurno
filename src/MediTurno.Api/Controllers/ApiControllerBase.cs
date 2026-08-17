using MediTurno.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Responder<T>(Resultado<T> resultado) =>
        resultado.Exitoso ? Ok(resultado.Valor) : Fallar(resultado);

    protected IActionResult ResponderCreado<T>(Resultado<T> resultado, string accion, object valoresRuta) =>
        resultado.Exitoso
            ? CreatedAtAction(accion, valoresRuta, resultado.Valor)
            : Fallar(resultado);

    protected IActionResult ResponderSinContenido<T>(Resultado<T> resultado) =>
        resultado.Exitoso ? NoContent() : Fallar(resultado);

    private IActionResult Fallar<T>(Resultado<T> resultado)
    {
        var cuerpo = new { mensaje = resultado.Mensaje };

        return resultado.Error switch
        {
            TipoError.Validacion => BadRequest(cuerpo),
            TipoError.NoEncontrado => NotFound(cuerpo),
            TipoError.Conflicto => Conflict(cuerpo),
            TipoError.NoAutorizado => Unauthorized(cuerpo),
            TipoError.Prohibido => StatusCode(StatusCodes.Status403Forbidden, cuerpo),
            _ => StatusCode(StatusCodes.Status500InternalServerError, cuerpo)
        };
    }
}
