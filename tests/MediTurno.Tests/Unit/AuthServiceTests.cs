using FluentAssertions;
using MediTurno.Api.Common;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using MediTurno.Tests.Helpers;

namespace MediTurno.Tests.Unit;

public class AuthServiceTests
{
    private const string Correo = "recepcion@mediturno.do";
    private const string Clave = "Recepcion123*";

    [Fact]
    public async Task Login_CuandoLasCredencialesSonCorrectas_DebeDevolverToken()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        var resultado = await escenario.Auth.LoginAsync(new LoginRequest { Correo = Correo, Password = Clave });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Token.Should().NotBeNullOrWhiteSpace();
        resultado.Valor.Rol.Should().Be(nameof(RolUsuario.Recepcionista));
        resultado.Valor.Expira.Should().Be(escenario.Ahora.AddMinutes(60));
    }

    [Fact]
    public async Task Login_CuandoLaContrasenaEsIncorrecta_DebeRetornarNoAutorizado()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        var resultado = await escenario.Auth.LoginAsync(
            new LoginRequest { Correo = Correo, Password = "claveErrada123" });

        resultado.Error.Should().Be(TipoError.NoAutorizado);
        resultado.Mensaje.Should().Be("Credenciales inválidas.");
    }

    [Fact]
    public async Task Login_CuandoElCorreoNoExiste_DebeDevolverElMismoMensajeGenerico()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        var inexistente = await escenario.Auth.LoginAsync(
            new LoginRequest { Correo = "nadie@mediturno.do", Password = Clave });

        var claveErrada = await escenario.Auth.LoginAsync(
            new LoginRequest { Correo = Correo, Password = "otra" });

        inexistente.Mensaje.Should().Be(claveErrada.Mensaje);
        inexistente.Error.Should().Be(TipoError.NoAutorizado);
    }

    [Fact]
    public async Task Login_CuandoElUsuarioEstaDesactivado_DebeRetornarProhibido()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista, activo: false);

        var resultado = await escenario.Auth.LoginAsync(new LoginRequest { Correo = Correo, Password = Clave });

        resultado.Error.Should().Be(TipoError.Prohibido);
    }

    [Fact]
    public void HashDeContrasena_NuncaDebeCoincidirConElTextoPlano()
    {
        using var escenario = new Escenario();
        var usuario = escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        usuario.PasswordHash.Should().NotBe(Clave);
        usuario.PasswordHash.Should().NotContain(Clave);
        usuario.PasswordHash.Length.Should().BeGreaterThan(40);
    }

    [Fact]
    public void HashDeContrasena_ParaLaMismaClave_DebeGenerarValoresDistintos()
    {
        using var escenario = new Escenario();
        var primero = escenario.CrearUsuario("uno@mediturno.do", Clave, RolUsuario.Recepcionista);
        var segundo = escenario.CrearUsuario("dos@mediturno.do", Clave, RolUsuario.Recepcionista);

        primero.PasswordHash.Should().NotBe(segundo.PasswordHash);
    }

    [Fact]
    public async Task CrearUsuario_CuandoElRolNoExiste_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Auth.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            NombreCompleto = "Nuevo Usuario",
            Correo = "nuevo@mediturno.do",
            Password = "Clave123*",
            Rol = "Enfermero"
        });

        resultado.Error.Should().Be(TipoError.Validacion);
        resultado.Mensaje.Should().Contain("Administrador");
    }

    [Fact]
    public async Task CrearUsuario_CuandoElCorreoYaExiste_DebeRetornarConflicto()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        var resultado = await escenario.Auth.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            NombreCompleto = "Otro Usuario",
            Correo = Correo.ToUpperInvariant(),
            Password = "Clave123*",
            Rol = "Recepcionista"
        });

        resultado.Error.Should().Be(TipoError.Conflicto);
    }

    [Fact]
    public async Task CrearUsuario_CuandoEsMedicoSinMedicoAsociado_DebeRetornarValidacion()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Auth.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            NombreCompleto = "Doctor Nuevo",
            Correo = "doctor@mediturno.do",
            Password = "Clave123*",
            Rol = "Medico"
        });

        resultado.Error.Should().Be(TipoError.Validacion);
    }

    [Fact]
    public async Task CrearUsuario_CuandoLosDatosSonValidos_DebeGuardarloConHash()
    {
        using var escenario = new Escenario();

        var resultado = await escenario.Auth.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            NombreCompleto = "Ana Recepción",
            Correo = "Ana@MediTurno.do",
            Password = "Clave123*",
            Rol = "recepcionista"
        });

        resultado.Exitoso.Should().BeTrue();
        resultado.Valor!.Correo.Should().Be("ana@mediturno.do");
        resultado.Valor.Rol.Should().Be(nameof(RolUsuario.Recepcionista));

        escenario.Db.Usuarios.Single().PasswordHash.Should().NotBe("Clave123*");
    }

    [Fact]
    public async Task ListarUsuarios_NuncaDebeExponerElHashDeContrasena()
    {
        using var escenario = new Escenario();
        escenario.CrearUsuario(Correo, Clave, RolUsuario.Recepcionista);

        var resultado = await escenario.Auth.ListarUsuariosAsync();

        resultado.Valor.Should().HaveCount(1);
        typeof(UsuarioResponse).GetProperty("PasswordHash").Should().BeNull();
    }
}
