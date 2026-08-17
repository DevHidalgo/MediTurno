using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Integration;

public class PacientesEndpointsTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private static CrearPacienteRequest Solicitud(string cedula) => new()
    {
        Cedula = cedula,
        Nombre = "Paciente",
        Apellido = "Integración",
        FechaNacimiento = new DateOnly(1990, 5, 20),
        Telefono = "8095550000",
        Correo = $"paciente{cedula}@correo.do"
    };

    [Fact]
    public async Task Crear_ConDatosValidos_DebeRetornarCreado()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("40299900001"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteResponse>();
        paciente!.Id.Should().BeGreaterThan(0);
        paciente.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task Crear_ConCedulaDuplicada_DebeRetornarConflicto()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("40299900002"));

        var respuesta = await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("40299900002"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Crear_ConCedulaDeFormatoInvalido_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("123"));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Crear_ConCorreoInvalido_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var solicitud = Solicitud("40299900003");
        solicitud.Correo = "esto-no-es-un-correo";

        var respuesta = await cliente.PostAsJsonAsync("/api/pacientes", solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Buscar_DebeRetornarResultadosPaginados()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.GetAsync("/api/pacientes?busqueda=&pagina=1&tamanoPagina=10");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var lista = await respuesta.Content.ReadFromJsonAsync<ListaPaginada<PacienteResponse>>();
        lista!.Pagina.Should().Be(1);
        lista.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ObtenerPorId_CuandoNoExiste_DebeRetornarNoEncontrado()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var respuesta = await cliente.GetAsync("/api/pacientes/99999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Desactivar_CuandoElPacienteExiste_DebeRetornarSinContenido()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var creacion = await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("40299900004"));
        var paciente = await creacion.Content.ReadFromJsonAsync<PacienteResponse>();

        var respuesta = await cliente.DeleteAsync($"/api/pacientes/{paciente!.Id}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Actualizar_DebeCambiarLosDatosDeContacto()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();

        var creacion = await cliente.PostAsJsonAsync("/api/pacientes", Solicitud("40299900005"));
        var paciente = await creacion.Content.ReadFromJsonAsync<PacienteResponse>();

        var respuesta = await cliente.PutAsJsonAsync($"/api/pacientes/{paciente!.Id}", new ActualizarPacienteRequest
        {
            Telefono = "8299991111",
            Correo = "actualizado@correo.do"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var actualizado = await respuesta.Content.ReadFromJsonAsync<PacienteResponse>();
        actualizado!.Telefono.Should().Be("8299991111");
    }
}
