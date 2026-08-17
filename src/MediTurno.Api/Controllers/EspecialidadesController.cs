using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/especialidades")]
[Authorize]
public class EspecialidadesController(ICatalogoService catalogoService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(EspecialidadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearEspecialidadRequest request)
    {
        var resultado = await catalogoService.CrearEspecialidadAsync(request);
        return ResponderCreado(resultado, nameof(Listar), new { });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EspecialidadResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() =>
        Responder(await catalogoService.ListarEspecialidadesAsync());

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id) =>
        ResponderSinContenido(await catalogoService.EliminarEspecialidadAsync(id));
}
