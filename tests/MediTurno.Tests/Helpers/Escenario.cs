using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Entities;
using MediTurno.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace MediTurno.Tests.Helpers;

public sealed class Escenario : IDisposable
{
    public static readonly DateTime LunesOchoAm = new(2026, 8, 17, 8, 0, 0);

    private readonly SqliteConnection _conexion;

    public Escenario(DateTime? ahora = null)
    {
        Ahora = ahora ?? LunesOchoAm;

        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        Db = new MediTurnoDbContext(
            new DbContextOptionsBuilder<MediTurnoDbContext>().UseSqlite(_conexion).Options);

        Db.Database.EnsureCreated();

        Reloj = new Mock<IRelojSistema>();
        Reloj.SetupGet(r => r.Ahora).Returns(Ahora);
    }

    public DateTime Ahora { get; }
    public MediTurnoDbContext Db { get; }
    public Mock<IRelojSistema> Reloj { get; }

    public IPasswordHasher<Usuario> Hasher { get; } = new PasswordHasher<Usuario>();

    public IAuthService Auth => new AuthService(
        Db,
        new TokenService(
            Options.Create(new JwtOptions
            {
                Issuer = "MediTurno.Pruebas",
                Audience = "MediTurno.Pruebas",
                Key = "clave-de-pruebas-mediturno-con-mas-de-32-bytes",
                MinutosVigencia = 60
            }),
            Reloj.Object),
        Hasher);

    public Usuario CrearUsuario(string correo, string password, RolUsuario rol, bool activo = true, int? medicoId = null)
    {
        var usuario = new Usuario
        {
            NombreCompleto = "Usuario Prueba",
            Correo = correo,
            Rol = rol,
            Activo = activo,
            MedicoId = medicoId
        };

        usuario.PasswordHash = Hasher.HashPassword(usuario, password);

        Db.Usuarios.Add(usuario);
        Db.SaveChanges();
        Db.ChangeTracker.Clear();

        return usuario;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conexion.Dispose();
    }
}
