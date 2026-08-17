using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Integration;

public class SeguridadEndpointsTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Login_ConCredencialesValidas_DebeRetornarOkConToken()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Correo = FabricaApi.CorreoAdministrador,
            Password = FabricaApi.ClaveAdministrador
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        login!.Token.Should().NotBeNullOrWhiteSpace();
        login.Rol.Should().Be("Administrador");
    }

    [Fact]
    public async Task Login_ConContrasenaIncorrecta_DebeRetornarNoAutorizado()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Correo = FabricaApi.CorreoAdministrador,
            Password = "claveErrada123"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("Credenciales inválidas");
        cuerpo.Should().NotContain("token");
    }

    [Fact]
    public async Task Login_SinContrasena_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { Correo = "admin@mediturno.do" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EndpointProtegido_SinToken_DebeRetornarNoAutorizado()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/pacientes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointProtegido_ConTokenManipulado_DebeRetornarNoAutorizado()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.falso.invalido");

        var respuesta = await cliente.GetAsync("/api/pacientes");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CrearMedico_ComoRecepcionista_DebeRetornarProhibido()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/medicos", new CrearMedicoRequest
        {
            NombreCompleto = "Dr. No Autorizado",
            Exequatur = "EXQ-9999",
            EspecialidadId = 1,
            DuracionConsultaMinutos = 30
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reportes_ComoMedico_DebeRetornarProhibido()
    {
        var cliente = await fabrica.ClienteMedicoAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/citas?desde=2026-08-01&hasta=2026-08-31");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Historial_ComoRecepcionista_DebeRetornarProhibido()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.GetAsync("/api/pacientes/1/historial");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CrearUsuario_ComoAdministradorConRolInvalido_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/usuarios", new CrearUsuarioRequest
        {
            NombreCompleto = "Usuario Invalido",
            Correo = "invalido@mediturno.do",
            Password = "Clave123*",
            Rol = "Enfermero"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ErrorNoControlado_DebeResponderConMensajeGenericoSinDetallesInternos()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/diagnostico/error");

        respuesta.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("Ocurrió un error inesperado");
        cuerpo.Should().NotContain("InvalidOperationException");
        cuerpo.Should().NotContain("MediTurno.Api.Controllers");
    }
}
