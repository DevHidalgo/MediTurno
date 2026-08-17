using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/diagnostico")]
[Authorize(Roles = "Administrador")]
public class DiagnosticoController : ApiControllerBase
{
    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ForzarError() =>
        throw new InvalidOperationException("Fallo simulado para verificar el manejo global de excepciones.");
}
