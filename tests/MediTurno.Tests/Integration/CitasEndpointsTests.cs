using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Integration;

public class CitasEndpointsTests(FabricaApi fabrica) : IClassFixture<FabricaApi>
{
    private const string ExequaturCarmen = "EXQ-1001";

    private static async Task<int> ObtenerMedicoAsync(HttpClient cliente)
    {
        var medicos = await cliente.GetFromJsonAsync<List<MedicoResponse>>("/api/medicos");
        return medicos!.First(m => m.Exequatur == ExequaturCarmen).Id;
    }

    private static async Task<int> ObtenerPacienteAsync(HttpClient cliente)
    {
        var pacientes = await cliente.GetFromJsonAsync<ListaPaginada<PacienteResponse>>("/api/pacientes");
        return pacientes!.Items[0].Id;
    }

    private static CrearCitaRequest Solicitud(int pacienteId, int medicoId, DateTime fechaHora) => new()
    {
        PacienteId = pacienteId,
        MedicoId = medicoId,
        FechaHora = fechaHora,
        MotivoConsulta = "Chequeo general de rutina"
    };

    [Fact]
    public async Task Reservar_ConBloqueDisponible_DebeRetornarCreado()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(8, 0))));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);

        var cita = await respuesta.Content.ReadFromJsonAsync<CitaResponse>();
        cita!.Estado.Should().Be("Pendiente");
    }

    [Fact]
    public async Task Reservar_EnUnBloqueYaOcupado_DebeRetornarConflicto()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacientes = await cliente.GetFromJsonAsync<ListaPaginada<PacienteResponse>>("/api/pacientes");
        var fechaHora = Fechas.ProximoDiaHabil(new TimeOnly(8, 30), 1);

        await cliente.PostAsJsonAsync("/api/citas", Solicitud(pacientes!.Items[0].Id, medicoId, fechaHora));

        var respuesta = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacientes.Items[1].Id, medicoId, fechaHora));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        cuerpo.Should().Contain("ya no está disponible");
    }

    [Fact]
    public async Task Reservar_FueraDelHorarioDeAtencion_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(16, 0))));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reservar_ConFechaPasada_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var respuesta = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, DateTime.Now.Date.AddDays(-3).AddHours(9)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirmar_UnaCitaPendiente_DebeRetornarOk()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var creacion = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(9, 0), 2)));
        var cita = await creacion.Content.ReadFromJsonAsync<CitaResponse>();

        var respuesta = await cliente.PutAsync($"/api/citas/{cita!.Id}/confirmar", null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmada = await respuesta.Content.ReadFromJsonAsync<CitaResponse>();
        confirmada!.Estado.Should().Be("Confirmada");
    }

    [Fact]
    public async Task Cancelar_SinIndicarMotivo_DebeRetornarSolicitudIncorrecta()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var creacion = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(9, 30), 3)));
        var cita = await creacion.Content.ReadFromJsonAsync<CitaResponse>();

        var respuesta = await cliente.PutAsJsonAsync($"/api/citas/{cita!.Id}/cancelar",
            new CancelarCitaRequest { Motivo = "" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reprogramar_UnaCitaCancelada_DebeRetornarConflicto()
    {
        var cliente = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(cliente);
        var pacienteId = await ObtenerPacienteAsync(cliente);

        var creacion = await cliente.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(10, 0), 4)));
        var cita = await creacion.Content.ReadFromJsonAsync<CitaResponse>();

        await cliente.PutAsJsonAsync($"/api/citas/{cita!.Id}/cancelar",
            new CancelarCitaRequest { Motivo = "El paciente canceló por viaje" });

        var respuesta = await cliente.PutAsJsonAsync($"/api/citas/{cita.Id}/reprogramar",
            new ReprogramarCitaRequest { NuevaFechaHora = Fechas.ProximoDiaHabil(new TimeOnly(10, 30), 4) });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RegistrarAtencion_ComoMedicoAsignadoEnCitaConfirmada_DebeRetornarCreado()
    {
        var recepcion = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(recepcion);
        var pacienteId = await ObtenerPacienteAsync(recepcion);

        var creacion = await recepcion.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(11, 0), 5)));
        var cita = await creacion.Content.ReadFromJsonAsync<CitaResponse>();

        await recepcion.PutAsync($"/api/citas/{cita!.Id}/confirmar", null);

        var medico = await fabrica.ClienteMedicoAsync();
        var respuesta = await medico.PostAsJsonAsync($"/api/citas/{cita.Id}/atencion", new RegistrarAtencionRequest
        {
            Motivo = "Control de presión arterial",
            Diagnostico = "Hipertensión controlada",
            Observaciones = "Continuar tratamiento actual"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegistrarAtencion_EnUnaCitaPendiente_DebeRetornarConflicto()
    {
        var recepcion = await fabrica.ClienteRecepcionistaAsync();
        var medicoId = await ObtenerMedicoAsync(recepcion);
        var pacienteId = await ObtenerPacienteAsync(recepcion);

        var creacion = await recepcion.PostAsJsonAsync("/api/citas",
            Solicitud(pacienteId, medicoId, Fechas.ProximoDiaHabil(new TimeOnly(11, 30), 6)));
        var cita = await creacion.Content.ReadFromJsonAsync<CitaResponse>();

        var medico = await fabrica.ClienteMedicoAsync();
        var respuesta = await medico.PostAsJsonAsync($"/api/citas/{cita!.Id}/atencion", new RegistrarAtencionRequest
        {
            Motivo = "Control",
            Diagnostico = "Sin hallazgos"
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RegistrarAtencion_SinDiagnostico_DebeRetornarSolicitudIncorrecta()
    {
        var medico = await fabrica.ClienteMedicoAsync();

        var respuesta = await medico.PostAsJsonAsync("/api/citas/1/atencion", new RegistrarAtencionRequest
        {
            Motivo = "Control general",
            Diagnostico = ""
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
