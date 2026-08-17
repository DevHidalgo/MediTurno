using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class CatalogoServiceTests
{
    [Fact]
    public async Task CrearEspecialidad_CuandoElNombreEsNuevo_DebeRegistrarla()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Catalogo.CrearEspecialidadAsync(
            new CrearEspecialidadRequest { Nombre = "Dermatología" });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Nombre.Should().Be("Dermatología");
    }

    [Fact]
    public async Task CrearEspecialidad_CuandoElNombreEstaDuplicado_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        await escenario.Catalogo.CrearEspecialidadAsync(new CrearEspecialidadRequest { Nombre = "Pediatría" });

        var resultado = await escenario.Catalogo.CrearEspecialidadAsync(
            new CrearEspecialidadRequest { Nombre = "Pediatría" });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task EliminarEspecialidad_CuandoTieneMedicosAsociados_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico();

        var resultado = await escenario.Catalogo.EliminarEspecialidadAsync(medico.EspecialidadId);

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task CrearMedico_CuandoLaEspecialidadNoExiste_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Catalogo.CrearMedicoAsync(new CrearMedicoRequest
        {
            NombreCompleto = "Dr. Prueba",
            Exequatur = "EXQ-5000",
            EspecialidadId = 999,
            DuracionConsultaMinutos = 30
        });

        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task CrearMedico_CuandoElExequaturEstaDuplicado_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        var existente = escenario.CrearMedico(exequatur: "EXQ-7777");

        var resultado = await escenario.Catalogo.CrearMedicoAsync(new CrearMedicoRequest
        {
            NombreCompleto = "Dr. Nuevo",
            Exequatur = "EXQ-7777",
            EspecialidadId = existente.EspecialidadId,
            DuracionConsultaMinutos = 30
        });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task DefinirHorarios_CuandoLaHoraFinEsAnteriorAlInicio_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico();

        var resultado = await escenario.Catalogo.DefinirHorariosAsync(medico.Id, new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest { DiaSemana = 2, HoraInicio = new TimeOnly(12, 0), HoraFin = new TimeOnly(8, 0) }
            ]
        });

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("posterior");
    }

    [Fact]
    public async Task DefinirHorarios_CuandoLosRangosSeSolapan_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico();

        var resultado = await escenario.Catalogo.DefinirHorariosAsync(medico.Id, new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest { DiaSemana = 2, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(12, 0) },
                new HorarioRequest { DiaSemana = 2, HoraInicio = new TimeOnly(11, 0), HoraFin = new TimeOnly(14, 0) }
            ]
        });

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("solapan");
    }

    [Fact]
    public async Task DefinirHorarios_CuandoHayDosRangosEnElMismoDiaSinSolape_DebeAceptarlos()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico();

        var resultado = await escenario.Catalogo.DefinirHorariosAsync(medico.Id, new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest { DiaSemana = 2, HoraInicio = new TimeOnly(8, 0), HoraFin = new TimeOnly(12, 0) },
                new HorarioRequest { DiaSemana = 2, HoraInicio = new TimeOnly(14, 0), HoraFin = new TimeOnly(17, 0) }
            ]
        });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor.Should().HaveCount(2);
    }

    [Fact]
    public async Task DefinirHorarios_DebeReemplazarPorCompletoLosHorariosAnteriores()
    {
        using var escenario = new Escenario();
        var medico = escenario.CrearMedico(dias: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday]);

        var resultado = await escenario.Catalogo.DefinirHorariosAsync(medico.Id, new DefinirHorariosRequest
        {
            Horarios =
            [
                new HorarioRequest { DiaSemana = 5, HoraInicio = new TimeOnly(9, 0), HoraFin = new TimeOnly(13, 0) }
            ]
        });

        resultado.Exitoso.Should().BeTrue();

        var horarios = await escenario.Catalogo.ObtenerHorariosAsync(medico.Id);
        horarios.Valor.Should().HaveCount(1);
        horarios.Valor![0].DiaSemana.Should().Be(5);
    }

    [Fact]
    public async Task DefinirHorarios_CuandoElMedicoNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Catalogo.DefinirHorariosAsync(999, new DefinirHorariosRequest());

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ObtenerMedico_CuandoNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Catalogo.ObtenerMedicoAsync(999);

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }
}
