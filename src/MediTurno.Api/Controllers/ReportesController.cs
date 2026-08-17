using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/reportes")]
[Authorize(Roles = "Administrador")]
public class ReportesController(IReporteService reporteService) : ApiControllerBase
{
    [HttpGet("citas")]
    [ProducesResponseType(typeof(ReporteCitasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Citas([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta) =>
        Responder(await reporteService.CitasPorEstadoAsync(desde, hasta));

    [HttpGet("medicos/{id:int}")]
    [ProducesResponseType(typeof(ReporteMedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PorMedico(int id, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta) =>
        Responder(await reporteService.PorMedicoAsync(id, desde, hasta));
}
