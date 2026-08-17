using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/citas")]
[Authorize]
public class CitasController(
    ICitaService citaService,
    IAtencionService atencionService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(CitaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reservar([FromBody] CrearCitaRequest request)
    {
        var resultado = await citaService.ReservarAsync(request);
        return ResponderCreado(resultado, nameof(ObtenerPorId), new { id = resultado.Valor?.Id ?? 0 });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CitaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int? medicoId,
        [FromQuery] int? pacienteId,
        [FromQuery] DateOnly? fecha) =>
        Responder(await citaService.ListarAsync(medicoId, pacienteId, fecha));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CitaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPorId(int id) =>
        Responder(await citaService.ObtenerPorIdAsync(id));

    [HttpPut("{id:int}/reprogramar")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(CitaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reprogramar(int id, [FromBody] ReprogramarCitaRequest request) =>
        Responder(await citaService.ReprogramarAsync(id, request));

    [HttpPut("{id:int}/cancelar")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(CitaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarCitaRequest request) =>
        Responder(await citaService.CancelarAsync(id, request));

    [HttpPut("{id:int}/confirmar")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(CitaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirmar(int id) =>
        Responder(await citaService.ConfirmarAsync(id));

    [HttpPost("marcar-ausentes")]
    [Authorize(Roles = "Administrador,Recepcionista")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarcarAusentes() =>
        Responder(await citaService.MarcarAusentesAsync());

    [HttpPost("{id:int}/atencion")]
    [Authorize(Roles = "Administrador,Medico")]
    [ProducesResponseType(typeof(AtencionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarAtencion(int id, [FromBody] RegistrarAtencionRequest request)
    {
        var esAdministrador = User.IsInRole("Administrador");
        var medicoId = int.TryParse(User.FindFirst("medicoId")?.Value, out var valor) ? valor : (int?)null;

        var resultado = await atencionService.RegistrarAsync(id, request, medicoId, esAdministrador);
        return ResponderCreado(resultado, nameof(ObtenerPorId), new { id });
    }
}
