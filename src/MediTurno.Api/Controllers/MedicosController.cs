using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/medicos")]
[Authorize]
public class MedicosController(
    ICatalogoService catalogoService,
    IDisponibilidadService disponibilidadService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearMedicoRequest request)
    {
        var resultado = await catalogoService.CrearMedicoAsync(request);
        return ResponderCreado(resultado, nameof(ObtenerPorId), new { id = resultado.Valor?.Id ?? 0 });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() =>
        Responder(await catalogoService.ListarMedicosAsync());

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        Responder(await catalogoService.ObtenerMedicoAsync(id));

    [HttpPut("{id:int}/horarios")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirHorarios(int id, [FromBody] DefinirHorariosRequest request) =>
        Responder(await catalogoService.DefinirHorariosAsync(id, request));

    [HttpGet("{id:int}/horarios")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHorarios(int id) =>
        Responder(await catalogoService.ObtenerHorariosAsync(id));

    [HttpGet("{id:int}/disponibilidad")]
    [ProducesResponseType(typeof(IReadOnlyList<BloqueDisponibleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibilidad(int id, [FromQuery] DateOnly fecha) =>
        Responder(await disponibilidadService.ObtenerDisponibilidadAsync(id, fecha));
}
