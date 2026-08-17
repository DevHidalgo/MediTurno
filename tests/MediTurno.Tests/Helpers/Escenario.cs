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
    public static readonly DateOnly Martes = new(2026, 8, 18);

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

    public IDisponibilidadService Disponibilidad => new DisponibilidadService(Db, Reloj.Object);
    public ICitaService Citas => new CitaService(Db, Disponibilidad, Reloj.Object);
    public IPacienteService Pacientes => new PacienteService(Db, Reloj.Object);
    public ICatalogoService Catalogo => new CatalogoService(Db);
    public IAtencionService Atenciones => new AtencionService(Db, Reloj.Object);
    public IReporteService Reportes => new ReporteService(Db);

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

    public Medico CrearMedico(
        int duracionMinutos = 30,
        TimeOnly? desde = null,
        TimeOnly? hasta = null,
        DayOfWeek[]? dias = null,
        string exequatur = "EXQ-9001")
    {
        var especialidad = new Especialidad { Nombre = $"Especialidad {exequatur}" };
        Db.Especialidades.Add(especialidad);
        Db.SaveChanges();

        var medico = new Medico
        {
            NombreCompleto = "Dra. Prueba",
            Exequatur = exequatur,
            EspecialidadId = especialidad.Id,
            DuracionConsultaMinutos = duracionMinutos,
            Activo = true
        };

        Db.Medicos.Add(medico);
        Db.SaveChanges();

        foreach (var dia in dias ?? [DayOfWeek.Tuesday])
        {
            Db.HorariosAtencion.Add(new HorarioAtencion
            {
                MedicoId = medico.Id,
                DiaSemana = dia,
                HoraInicio = desde ?? new TimeOnly(8, 0),
                HoraFin = hasta ?? new TimeOnly(12, 0)
            });
        }

        Db.SaveChanges();
        Db.ChangeTracker.Clear();

        return Db.Medicos.Include(m => m.Horarios).First(m => m.Id == medico.Id);
    }

    public Paciente CrearPaciente(string cedula = "40200000001", bool activo = true)
    {
        var paciente = new Paciente
        {
            Cedula = cedula,
            Nombre = "Paciente",
            Apellido = "Prueba",
            FechaNacimiento = new DateOnly(1990, 1, 1),
            Telefono = "8090000000",
            Correo = $"{cedula}@correo.do",
            Activo = activo,
            FechaRegistro = Ahora
        };

        Db.Pacientes.Add(paciente);
        Db.SaveChanges();

        return paciente;
    }

    public Cita CrearCita(int pacienteId, int medicoId, DateTime fechaHora, EstadoCita estado = EstadoCita.Pendiente)
    {
        var cita = new Cita
        {
            PacienteId = pacienteId,
            MedicoId = medicoId,
            FechaHora = fechaHora,
            Estado = estado,
            MotivoConsulta = "Consulta de prueba",
            FechaCreacion = Ahora
        };

        Db.Citas.Add(cita);
        Db.SaveChanges();
        Db.ChangeTracker.Clear();

        return cita;
    }

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
