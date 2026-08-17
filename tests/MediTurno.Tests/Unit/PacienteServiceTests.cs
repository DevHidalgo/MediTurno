using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Dtos;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class PacienteServiceTests
{
    private static CrearPacienteRequest Solicitud(string cedula = "40211112222") => new()
    {
        Cedula = cedula,
        Nombre = "Ana",
        Apellido = "Martínez",
        FechaNacimiento = new DateOnly(1992, 4, 15),
        Telefono = "8095551234",
        Correo = "Ana.Martinez@Correo.do"
    };

    [Fact]
    public async Task Crear_CuandoLosDatosSonValidos_DebeRegistrarloComoActivo()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Pacientes.CrearAsync(Solicitud());

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Activo.Should().BeTrue();
        resultado.Valor.Correo.Should().Be("ana.martinez@correo.do");
        resultado.Valor.FechaRegistro.Should().Be(escenario.Ahora);
    }

    [Fact]
    public async Task Crear_CuandoLaCedulaYaExiste_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        await escenario.Pacientes.CrearAsync(Solicitud());

        var resultado = await escenario.Pacientes.CrearAsync(Solicitud());

        resultado.Error.Should().Be(TipoError.Conflicto);
        resultado.Mensaje.Should().Be("Ya existe un paciente con esa cédula.");
    }

    [Fact]
    public async Task Crear_CuandoLaFechaDeNacimientoEsFutura_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();
        var solicitud = Solicitud();
        solicitud.FechaNacimiento = DateOnly.FromDateTime(escenario.Ahora).AddDays(1);

        var resultado = await escenario.Pacientes.CrearAsync(solicitud);

        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task Buscar_PorCedula_DebeEncontrarAlPaciente()
    {
        using var escenario = new Escenario();
        await escenario.Pacientes.CrearAsync(Solicitud("40211112222"));
        await escenario.Pacientes.CrearAsync(Solicitud("00133334444"));

        var resultado = await escenario.Pacientes.BuscarAsync("40211112222", false, 1, 20);

        resultado.Valor!.Items.Should().HaveCount(1);
        resultado.Valor.Items[0].Cedula.Should().Be("40211112222");
    }

    [Fact]
    public async Task Buscar_PorNombreParcial_DebeIgnorarMayusculasYMinusculas()
    {
        using var escenario = new Escenario();
        await escenario.Pacientes.CrearAsync(Solicitud());

        var resultado = await escenario.Pacientes.BuscarAsync("ANA", false, 1, 20);

        resultado.Valor!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Buscar_CuandoNoHayCoincidencias_DebeDevolverListaVaciaYNoUnError()
    {
        using var escenario = new Escenario();
        await escenario.Pacientes.CrearAsync(Solicitud());

        var resultado = await escenario.Pacientes.BuscarAsync("inexistente", false, 1, 20);

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Items.Should().BeEmpty();
        resultado.Valor.Total.Should().Be(0);
    }

    [Fact]
    public async Task Buscar_CuandoHayMasResultadosQueLaPagina_DebePaginar()
    {
        using var escenario = new Escenario();

        for (var i = 0; i < 25; i++)
        {
            escenario.CrearPaciente($"402000000{i:D2}");
        }

        var resultado = await escenario.Pacientes.BuscarAsync(null, false, 1, 20);

        resultado.Valor!.Items.Should().HaveCount(20);
        resultado.Valor.Total.Should().Be(25);
        resultado.Valor.Pagina.Should().Be(1);
    }

    [Fact]
    public async Task Buscar_PorDefecto_DebeExcluirLosPacientesInactivos()
    {
        using var escenario = new Escenario();
        escenario.CrearPaciente("40200000001");
        escenario.CrearPaciente("40200000002", activo: false);

        var soloActivos = await escenario.Pacientes.BuscarAsync(null, false, 1, 20);
        var todos = await escenario.Pacientes.BuscarAsync(null, true, 1, 20);

        soloActivos.Valor!.Total.Should().Be(1);
        todos.Valor!.Total.Should().Be(2);
    }

    [Fact]
    public async Task Actualizar_CuandoElPacienteNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Pacientes.ActualizarAsync(999, new ActualizarPacienteRequest
        {
            Telefono = "8090000000",
            Correo = "nuevo@correo.do"
        });

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task Actualizar_CuandoElPacienteExiste_DebeCambiarLosDatosDeContacto()
    {
        using var escenario = new Escenario();
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Pacientes.ActualizarAsync(paciente.Id, new ActualizarPacienteRequest
        {
            Telefono = "8299998888",
            Correo = "actualizado@correo.do"
        });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Telefono.Should().Be("8299998888");
        resultado.Valor.Cedula.Should().Be(paciente.Cedula);
    }

    [Fact]
    public async Task Desactivar_CuandoElPacienteExiste_DebeAplicarBajaLogica()
    {
        using var escenario = new Escenario();
        var paciente = escenario.CrearPaciente();

        var resultado = await escenario.Pacientes.DesactivarAsync(paciente.Id);

        resultado.Exitoso.Should().BeTrue();
        escenario.Db.Pacientes.Single().Activo.Should().BeFalse();
    }

    [Fact]
    public async Task Desactivar_CuandoElPacienteNoExiste_DebeRetornarNoEncontrado()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Pacientes.DesactivarAsync(999);

        resultado.Error.Should().Be(TipoError.NoEncontrado);
    }
}
