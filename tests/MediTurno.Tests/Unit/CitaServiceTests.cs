using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class CitaServiceTests
{
    private static CrearCitaRequest Solicitud(int pacienteId, int medicoId, DateTime fechaHora) => new()
    {
        PacienteId = pacienteId,
        MedicoId = medicoId,
        FechaHora = fechaHora,
        MotivoConsulta = "Dolor de cabeza persistente"
    };

    [Fact]
    public async Task ReservarCita_CuandoElBloqueEstaLibre_DebeCrearlaEnEstadoPendiente()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0))));

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Estado.Should().Be(nameof(EstadoCita.Pendiente));
        resultado.Valor.FechaHora.Should().Be(Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));
    }

    [Fact]
    public async Task ReservarCita_CuandoElBloqueEstaOcupado_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var primero = escenario.CrearPaciente("40200000001");
        var segundo = escenario.CrearPaciente("40200000002");
        var fechaHora = Escenario.Martes.ToDateTime(new TimeOnly(9, 0));

        escenario.CrearCita(primero.Id, medico.Id, fechaHora);

        var resultado = await escenario.Citas.ReservarAsync(Solicitud(segundo.Id, medico.Id, fechaHora));

        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().Be(TipoError.Conflicto);
        resultado.Mensaje.Should().Be("El horario seleccionado ya no está disponible.");
    }

    [Fact]
    public async Task ReservarCita_CuandoLaCitaPreviaEstaCancelada_DebePermitirReservar()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var primero = escenario.CrearPaciente("40200000001");
        var segundo = escenario.CrearPaciente("40200000002");
        var fechaHora = Escenario.Martes.ToDateTime(new TimeOnly(9, 0));

        escenario.CrearCita(primero.Id, medico.Id, fechaHora, EstadoCita.Cancelada);

        var resultado = await escenario.Citas.ReservarAsync(Solicitud(segundo.Id, medico.Id, fechaHora));

        resultado.Exitoso.Should().BeTrue();
    }

    [Fact]
    public async Task ReservarCita_CuandoLaFechaEsPasada_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, new DateTime(2026, 8, 17, 7, 0, 0)));

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("futuras");
    }

    [Fact]
    public async Task ReservarCita_CuandoNoRespetaLaAntelacionMinima_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, new DateTime(2026, 8, 17, 9, 0, 0)));

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("antelación");
    }

    [Fact]
    public async Task ReservarCita_CuandoCumpleExactamenteLaAntelacionMinima_DebeAceptarla()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, new DateTime(2026, 8, 17, 10, 0, 0)));

        resultado.Exitoso.Should().BeTrue();
    }

    [Fact]
    public async Task ReservarCita_CuandoElHorarioEstaFueraDeLaAgenda_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(15, 0))));

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("bloque de atención");
    }

    [Fact]
    public async Task ReservarCita_CuandoElHorarioNoCoincideConUnBloque_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 15))));

        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task ReservarCita_CuandoElPacienteEstaInactivo_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente(activo: false);

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0))));

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task ReservarCita_CuandoElPacienteNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(999, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0))));

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ReservarCita_CuandoElMedicoNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Citas.ReservarAsync(
            Solicitud(paciente.Id, 999, Escenario.Martes.ToDateTime(new TimeOnly(9, 0))));

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ReservarCita_CuandoElPacienteYaTieneCitaEnEseHorario_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var primero = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-1");
        var segundo = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-2");
        var paciente = escenario.CrearPaciente();
        var fechaHora = Escenario.Martes.ToDateTime(new TimeOnly(9, 0));

        escenario.CrearCita(paciente.Id, primero.Id, fechaHora);

        var resultado = await escenario.Citas.ReservarAsync(Solicitud(paciente.Id, segundo.Id, fechaHora));

        resultado.Error.Should().Be(TipoError.Conflicto);
        resultado.Mensaje.Should().Contain("paciente");
    }

    [Fact]
    public async Task Reprogramar_CuandoElNuevoBloqueEstaLibre_DebeActualizarLaFecha()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));

        var nuevaFecha = Escenario.Martes.ToDateTime(new TimeOnly(11, 0));
        var resultado = await escenario.Citas.ReprogramarAsync(
            cita.Id, new ReprogramarCitaRequest { NuevaFechaHora = nuevaFecha });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.FechaHora.Should().Be(nuevaFecha);

        var disponibilidad = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);
        disponibilidad.Valor!.Should().Contain(b => b.Hora == new TimeOnly(9, 0));
    }

    [Fact]
    public async Task Reprogramar_CuandoElNuevoBloqueEstaOcupado_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var primero = escenario.CrearPaciente("40200000001");
        var segundo = escenario.CrearPaciente("40200000002");

        var cita = escenario.CrearCita(primero.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));
        escenario.CrearCita(segundo.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(10, 0)));

        var resultado = await escenario.Citas.ReprogramarAsync(
            cita.Id,
            new ReprogramarCitaRequest { NuevaFechaHora = Escenario.Martes.ToDateTime(new TimeOnly(10, 0)) });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Theory]
    [InlineData(EstadoCita.Atendida)]
    [InlineData(EstadoCita.Cancelada)]
    [InlineData(EstadoCita.Ausente)]
    public async Task Reprogramar_CuandoLaCitaNoEstaVigente_DebeRetornarConflicto(EstadoCita estado)
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), estado);

        var resultado = await escenario.Citas.ReprogramarAsync(
            cita.Id,
            new ReprogramarCitaRequest { NuevaFechaHora = Escenario.Martes.ToDateTime(new TimeOnly(11, 0)) });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task Reprogramar_CuandoLaCitaNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Citas.ReprogramarAsync(
            999, new ReprogramarCitaRequest { NuevaFechaHora = Escenario.Martes.ToDateTime(new TimeOnly(9, 0)) });

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task Cancelar_CuandoLaCitaEstaVigente_DebeRegistrarElMotivo()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));

        var resultado = await escenario.Citas.CancelarAsync(
            cita.Id, new CancelarCitaRequest { Motivo = "El paciente viajó fuera del país" });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Estado.Should().Be(nameof(EstadoCita.Cancelada));
        resultado.Valor.MotivoCancelacion.Should().Be("El paciente viajó fuera del país");
        resultado.Valor.FechaCancelacion.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EstadoCita.Atendida)]
    [InlineData(EstadoCita.Cancelada)]
    public async Task Cancelar_CuandoLaCitaNoEstaVigente_DebeRetornarConflicto(EstadoCita estado)
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), estado);

        var resultado = await escenario.Citas.CancelarAsync(
            cita.Id, new CancelarCitaRequest { Motivo = "Motivo cualquiera" });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task Confirmar_CuandoLaCitaEstaPendiente_DebePasarAConfirmada()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));

        var resultado = await escenario.Citas.ConfirmarAsync(cita.Id);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Estado.Should().Be(nameof(EstadoCita.Confirmada));
        resultado.Valor.FechaConfirmacion.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EstadoCita.Confirmada)]
    [InlineData(EstadoCita.Cancelada)]
    [InlineData(EstadoCita.Atendida)]
    public async Task Confirmar_CuandoLaCitaNoEstaPendiente_DebeRetornarConflicto(EstadoCita estado)
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), estado);

        var resultado = await escenario.Citas.ConfirmarAsync(cita.Id);

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task MarcarAusentes_CuandoLaCitaConfirmadaYaPaso_DebeMarcarlaComoAusente()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday]);
        var paciente = escenario.CrearPaciente();
        escenario.CrearCita(
            paciente.Id, medico.Id, new DateTime(2026, 8, 17, 6, 0, 0), EstadoCita.Confirmada);

        var resultado = await escenario.Citas.MarcarAusentesAsync();

        resultado.Valor.Should().Be(1);
        escenario.Db.Citas.Single().Estado.Should().Be(EstadoCita.Ausente);
    }

    [Fact]
    public async Task Listar_CuandoSeFiltraPorMedico_DebeDevolverSoloSusCitas()
    {
        using var escenario = new Escenario();
        var primero = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-1");
        var segundo = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-2");
        var paciente = escenario.CrearPaciente();

        escenario.CrearCita(paciente.Id, primero.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));
        escenario.CrearCita(paciente.Id, segundo.Id, Escenario.Martes.ToDateTime(new TimeOnly(10, 0)));

        var resultado = await escenario.Citas.ListarAsync(primero.Id, null, null);

        resultado.Valor.Should().HaveCount(1);
        resultado.Valor!.Single().MedicoId.Should().Be(primero.Id);
    }
}
