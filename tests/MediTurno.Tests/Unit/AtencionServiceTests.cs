using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class AtencionServiceTests
{
    private static RegistrarAtencionRequest Solicitud() => new()
    {
        Motivo = "Dolor abdominal",
        Diagnostico = "Gastritis aguda",
        Observaciones = "Se indica dieta blanda por siete días"
    };

    [Fact]
    public async Task Registrar_CuandoLaCitaEstaConfirmada_DebeMarcarlaComoAtendida()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), EstadoCita.Confirmada);

        var resultado = await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), medico.Id, false);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Diagnostico.Should().Be("Gastritis aguda");
        escenario.Db.Citas.Single().Estado.Should().Be(EstadoCita.Atendida);
    }

    [Fact]
    public async Task Registrar_CuandoLaCitaEstaPendiente_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));

        var resultado = await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), medico.Id, false);

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task Registrar_CuandoElMedicoNoEsElAsignado_DebeRetornarProhibido()
    {
        using var escenario = new Escenario();
        var asignado = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-1");
        var otro = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-2");
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, asignado.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), EstadoCita.Confirmada);

        var resultado = await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), otro.Id, false);

        resultado.Error.Should().Be(TipoError.Prohibido);
    }

    [Fact]
    public async Task Registrar_CuandoEsAdministrador_DebePermitirloAunSinSerElMedicoAsignado()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), EstadoCita.Confirmada);

        var resultado = await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), null, true);

        resultado.Exitoso.Should().BeTrue();
    }

    [Fact]
    public async Task Registrar_CuandoLaCitaYaFueAtendida_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), EstadoCita.Confirmada);

        await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), medico.Id, false);
        var resultado = await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), medico.Id, false);

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task Registrar_CuandoLaCitaNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Atenciones.RegistrarAsync(999, Solicitud(), 1, false);

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerHistorial_DebeOrdenarDeLaAtencionMasRecienteALaMasAntigua()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        var antigua = escenario.CrearCita(
            paciente.Id, medico.Id, new DateTime(2026, 6, 2, 9, 0, 0), EstadoCita.Confirmada);
        var reciente = escenario.CrearCita(
            paciente.Id, medico.Id, new DateTime(2026, 7, 7, 9, 0, 0), EstadoCita.Confirmada);

        await escenario.Atenciones.RegistrarAsync(antigua.Id, Solicitud(), medico.Id, false);
        await escenario.Atenciones.RegistrarAsync(reciente.Id, Solicitud(), medico.Id, false);

        var resultado = await escenario.Atenciones.ObtenerHistorialAsync(paciente.Id);

        resultado.Valor.Should().HaveCount(2);
        resultado.Valor![0].Fecha.Should().Be(new DateTime(2026, 7, 7, 9, 0, 0));
        resultado.Valor[1].Fecha.Should().Be(new DateTime(2026, 6, 2, 9, 0, 0));
    }

    [Fact]
    public async Task ObtenerHistorial_CuandoElPacienteNoTieneAtenciones_DebeDevolverListaVacia()
    {
        using var escenario = new Escenario();
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Atenciones.ObtenerHistorialAsync(paciente.Id);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerHistorial_CuandoElPacienteNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Atenciones.ObtenerHistorialAsync(999);

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerHistorial_CuandoElPacienteEstaInactivo_DebeSeguirDisponible()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        var cita = escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)), EstadoCita.Confirmada);

        await escenario.Atenciones.RegistrarAsync(cita.Id, Solicitud(), medico.Id, false);
        await escenario.Pacientes.DesactivarAsync(paciente.Id);

        var resultado = await escenario.Atenciones.ObtenerHistorialAsync(paciente.Id);

        resultado.Valor.Should().HaveCount(1);
    }
}
