using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Integration;

public class ReportesEndpointsTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Citas_ComoAdministrador_DebeRetornarLosContadoresPorEstado()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/citas?desde=2026-01-01&hasta=2026-12-31");

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var reporte = await respuesta.Content.ReadFromJsonAsync<ReporteCitasResponse>();
        reporte!.Total.Should().Be(
            reporte.Pendientes + reporte.Confirmadas + reporte.Atendidas + reporte.Canceladas + reporte.Ausentes);
    }

    [Fact]
    public async Task Citas_ConFechaDeInicioPosteriorALaDeFin_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/citas?desde=2026-12-31&hasta=2026-01-01");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Citas_ConRangoSuperiorAUnAno_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/citas?desde=2024-01-01&hasta=2026-12-31");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PorMedico_CuandoElMedicoNoExiste_DebeRetornarNoEncontrado()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.GetAsync("/api/reportes/medicos/9999?desde=2026-01-01&hasta=2026-12-31");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
