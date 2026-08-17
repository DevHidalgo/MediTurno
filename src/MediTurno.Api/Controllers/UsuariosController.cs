using MediTurno.Api.Dtos;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediTurno.Api.Controllers;

[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController(IAuthService authService) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request)
    {
        var resultado = await authService.CrearUsuarioAsync(request);
        return ResponderCreado(resultado, nameof(Listar), new { });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() =>
        Responder(await authService.ListarUsuariosAsync());
}
