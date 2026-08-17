using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Entities;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class ReporteServiceTests
{
    private static readonly DateOnly Desde = new(2026, 8, 1);
    private static readonly DateOnly Hasta = new(2026, 8, 31);

    [Fact]
    public async Task CitasPorEstado_DebeAgruparCorrectamenteCadaEstado()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 8, 0, 0), EstadoCita.Pendiente);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 8, 30, 0), EstadoCita.Confirmada);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 9, 0, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 9, 30, 0), EstadoCita.Cancelada);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 10, 0, 0), EstadoCita.Ausente);

        var resultado = await escenario.Reportes.CitasPorEstadoAsync(Desde, Hasta);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Pendientes.Should().Be(1);
        resultado.Valor.Confirmadas.Should().Be(1);
        resultado.Valor.Atendidas.Should().Be(1);
        resultado.Valor.Canceladas.Should().Be(1);
        resultado.Valor.Ausentes.Should().Be(1);
        resultado.Valor.Total.Should().Be(5);
    }

    [Fact]
    public async Task CitasPorEstado_CuandoLaFechaDeInicioEsPosteriorALaDeFin_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Reportes.CitasPorEstadoAsync(Hasta, Desde);

        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task CitasPorEstado_CuandoElRangoSuperaUnAno_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Reportes.CitasPorEstadoAsync(
            new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31));

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("365");
    }

    [Fact]
    public async Task CitasPorEstado_CuandoNoHayCitasEnElRango_DebeDevolverTodosLosContadoresEnCero()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Reportes.CitasPorEstadoAsync(Desde, Hasta);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Total.Should().Be(0);
        resultado.Valor.Atendidas.Should().Be(0);
    }

    [Fact]
    public async Task CitasPorEstado_DebeExcluirLasCitasFueraDelRango()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 9, 0, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 9, 15, 9, 0, 0), EstadoCita.Atendida);

        var resultado = await escenario.Reportes.CitasPorEstadoAsync(Desde, Hasta);

        resultado.Valor!.Total.Should().Be(1);
    }

    [Fact]
    public async Task PorMedico_DebeCalcularLaTasaDeAusentismo()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();

        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 8, 0, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 8, 30, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 9, 0, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, medico.Id, new DateTime(2026, 8, 18, 9, 30, 0), EstadoCita.Ausente);

        var resultado = await escenario.Reportes.PorMedicoAsync(medico.Id, Desde, Hasta);

        resultado.Valor!.Atendidas.Should().Be(3);
        resultado.Valor.Ausentes.Should().Be(1);
        resultado.Valor.TasaAusentismo.Should().Be(25m);
    }

    [Fact]
    public async Task PorMedico_CuandoNoHayCitasCerradas_LaTasaDebeSerCero()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Tuesday]);

        var resultado = await escenario.Reportes.PorMedicoAsync(medico.Id, Desde, Hasta);

        resultado.Valor!.TasaAusentismo.Should().Be(0m);
    }

    [Fact]
    public async Task PorMedico_CuandoElMedicoNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Reportes.PorMedicoAsync(999, Desde, Hasta);

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task PorMedico_DebeExcluirLasCitasDeOtrosMedicos()
    {
        using var escenario = new Escenario();
        var primero = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-1");
        var segundo = escenario.CrearMedico(dias: [DayOfWeek.Tuesday], exequatur: "EXQ-2");
        var paciente = escenario.CrearPaciente();

        escenario.CrearCita(paciente.Id, primero.Id, new DateTime(2026, 8, 18, 8, 0, 0), EstadoCita.Atendida);
        escenario.CrearCita(paciente.Id, segundo.Id, new DateTime(2026, 8, 18, 8, 30, 0), EstadoCita.Atendida);

        var resultado = await escenario.Reportes.PorMedicoAsync(primero.Id, Desde, Hasta);

        resultado.Valor!.Total.Should().Be(1);
    }
}
