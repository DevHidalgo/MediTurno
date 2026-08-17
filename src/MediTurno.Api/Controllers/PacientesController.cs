using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/pacientes")]
[Authorize]
public class PacientesController(IPacienteService pacienteService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearPacienteRequest request)
    {
        var resultado = await pacienteService.CrearAsync(request);
        return ResponderCreado(resultado, nameof(ObtenerPorId), new { id = resultado.Valor?.Id ?? 0 });
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Recepcionista,Medico")]
    [ProducesResponseType(typeof(ListaPaginada<PacienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? busqueda,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20) =>
        Responder(await pacienteService.BuscarAsync(busqueda, incluirInactivos, pagina, tamanoPagina));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrador,Recepcionista,Medico")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        Responder(await pacienteService.ObtenerPorIdAsync(id));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarPacienteRequest request) =>
        Responder(await pacienteService.ActualizarAsync(id, request));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id) =>
        ResponderSinContenido(await pacienteService.DesactivarAsync(id));
}
