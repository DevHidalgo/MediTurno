using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Integration;

public class MedicosEndpointsTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task CrearEspecialidad_ComoAdministrador_DebeRetornarCreado()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/especialidades",
            new CrearEspecialidadRequest { Nombre = "Neurología" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CrearEspecialidad_ConNombreDuplicado_DebeRetornarConflicto()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/especialidades",
            new CrearEspecialidadRequest { Nombre = "Medicina General" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CrearMedico_ConDuracionFueraDeRango_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/medicos", new CrearMedicoRequest
        {
            NombreCompleto = "Dr. Duración Inválida",
            Exequatur = "EXQ-8001",
            EspecialidadId = 1,
            DuracionConsultaMinutos = 5
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearMedicoYDefinirHorarios_DebeReflejarseEnLaDisponibilidad()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var creacion = await cliente.PostAsJsonAsync("/api/medicos", new CrearMedicoRequest
        {
            NombreCompleto = "Dr. Integración",
            Exequatur = "EXQ-8002",
            EspecialidadId = 1,
            DuracionConsultaMinutos = 60
        });

        creacion.StatusCode.Should().Be(HttpStatusCode.Created);
        var medico = await creacion.Content.ReadFromJsonAsync<MedicoResponse>();

        var fecha = DateOnly.FromDateTime(Fechas.ProximoDiaHabil(new TimeOnly(9, 0)));

        var horarios = await cliente.PutAsJsonAsync($"/api/medicos/{medico!.Id}/horarios", new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest
                {
                    DiaSemana = (int)fecha.DayOfWeek,
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFin = new TimeOnly(13, 0)
                }
            ]
        });

        horarios.StatusCode.Should().Be(HttpStatusCode.OK);

        var bloques = await cliente.GetFromJsonAsync<List<BloqueDisponibleResponse>>(
            $"/api/medicos/{medico.Id}/disponibilidad?fecha={fecha:yyyy-MM-dd}");

        bloques.Should().HaveCount(4);
        bloques![0].Hora.Should().Be(new TimeOnly(9, 0));
    }

    [Fact]
    public async Task DefinirHorarios_ConRangoInvertido_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteAdministradorAsync();

        var respuesta = await cliente.PutAsJsonAsync("/api/medicos/1/horarios", new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest { DiaSemana = 1, HoraInicio = new TimeOnly(15, 0), HoraFin = new TimeOnly(9, 0) }
            ]
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Disponibilidad_ConFechaPasada_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var ayer = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

        var respuesta = await cliente.GetAsync($"/api/medicos/1/disponibilidad?fecha={ayer:yyyy-MM-dd}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Disponibilidad_DeUnMedicoInexistente_DebeRetornarNoEncontrado()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var fecha = DateOnly.FromDateTime(Fechas.ProximoDiaHabil(new TimeOnly(9, 0)));

        var respuesta = await cliente.GetAsync($"/api/medicos/9999/disponibilidad?fecha={fecha:yyyy-MM-dd}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
