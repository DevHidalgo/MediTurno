using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Entities;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class DisponibilidadServiceTests
{
    [Fact]
    public async Task ObtenerDisponibilidad_CuandoNoHayCitas_DebeGenerarTodosLosBloques()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Tuesday]);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor.Should().HaveCount(8);
        resultado.Valor!.First().Hora.Should().Be(new TimeOnly(8, 0));
        resultado.Valor!.Last().Hora.Should().Be(new TimeOnly(11, 30));
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoElBloqueEstaOcupado_DebeExcluirlo()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        escenario.CrearCita(paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(9, 0)));

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Valor.Should().HaveCount(7);
        resultado.Valor!.Should().NotContain(b => b.Hora == new TimeOnly(9, 0));
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoLaCitaEstaCancelada_DebeIncluirElBloque()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Tuesday]);
        var paciente = escenario.CrearPaciente();
        escenario.CrearCita(
            paciente.Id, medico.Id, Escenario.Martes.ToDateTime(new TimeOnly(10, 0)), EstadoCita.Cancelada);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Valor.Should().HaveCount(8);
        resultado.Valor!.Should().Contain(b => b.Hora == new TimeOnly(10, 0));
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoElMedicoNoAtiendeEseDia_DebeDevolverListaVacia()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday]);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoLaFechaEsPasada_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico();

        var ayer = DateOnly.FromDateTime(escenario.Ahora).AddDays(-1);
        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, ayer);

        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoElMedicoNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(999, Escenario.Martes);

        resultado.Exitoso.Should().BeFalse();
        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoEsHoy_DebeExcluirLosBloquesYaTranscurridos()
    {
        using var escenario = new Escenario(new DateTime(2026, 8, 17, 9, 45, 0));
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Monday]);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(
            medico.Id, new DateOnly(2026, 8, 17));

        resultado.Valor.Should().HaveCount(4);
        resultado.Valor!.First().Hora.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task ObtenerDisponibilidad_CuandoLaConsultaDuraVeinteMinutos_DebeGenerarNueveBloques()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(
            duracionMinutos: 20,
            desde: new TimeOnly(14, 0),
            hasta: new TimeOnly(17, 0),
            dias: [DayOfWeek.Tuesday]);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Valor.Should().HaveCount(9);
    }

    [Fact]
    public void GenerarBloques_CuandoElUltimoBloqueNoCabeCompleto_DebeDescartarlo()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(
            duracionMinutos: 30,
            desde: new TimeOnly(8, 0),
            hasta: new TimeOnly(9, 20),
            dias: [DayOfWeek.Tuesday]);

        var bloques = escenario.Disponibilidad.GenerarBloques(medico, Escenario.Martes);

        bloques.Should().HaveCount(2);
        bloques.Last().TimeOfDay.Should().Be(new TimeSpan(8, 30, 0));
    }

    [Fact]
    public void GenerarBloques_CuandoLaDuracionEsCero_DebeDevolverListaVacia()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: 30, dias: [DayOfWeek.Tuesday]);
        medico.DuracionConsultaMinutos = 0;

        var bloques = escenario.Disponibilidad.GenerarBloques(medico, Escenario.Martes);

        bloques.Should().BeEmpty();
    }

    [Theory]
    [InlineData(30, 8)]
    [InlineData(60, 4)]
    [InlineData(15, 16)]
    public async Task ObtenerDisponibilidad_SegunLaDuracionDeConsulta_DebeGenerarLaCantidadEsperada(
        int duracion, int bloquesEsperados)
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(duracionMinutos: duracion, dias: [DayOfWeek.Tuesday]);

        var resultado = await escenario.Disponibilidad.ObtenerDisponibilidadAsync(medico.Id, Escenario.Martes);

        resultado.Valor.Should().HaveCount(bloquesEsperados);
    }
}
