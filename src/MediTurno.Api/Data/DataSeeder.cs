using MediTurno.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Data;

public static class DataSeeder
{
    public static async Task SembrarAsync(MediTurnoDbContext db, IPasswordHasher<Usuario> hasher)
    {
        if (await db.Usuarios.AnyAsync())
        {
            return;
        }

        var general = new Especialidad { Nombre = "Medicina General" };
        var pediatria = new Especialidad { Nombre = "Pediatría" };
        var cardiologia = new Especialidad { Nombre = "Cardiología" };

        db.Especialidades.AddRange(general, pediatria, cardiologia);
        await db.SaveChangesAsync();

        var carmen = new Medico
        {
            NombreCompleto = "Dra. Carmen Reyes",
            Exequatur = "EXQ-1001",
            EspecialidadId = general.Id,
            DuracionConsultaMinutos = 30
        };

        var luis = new Medico
        {
            NombreCompleto = "Dr. Luis Peña",
            Exequatur = "EXQ-1002",
            EspecialidadId = pediatria.Id,
            DuracionConsultaMinutos = 20
        };

        db.Medicos.AddRange(carmen, luis);
        await db.SaveChangesAsync();

        var horarios = new List<HorarioAtencion>();

        foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            horarios.Add(new HorarioAtencion
            {
                MedicoId = carmen.Id,
                DiaSemana = dia,
                HoraInicio = new TimeOnly(8, 0),
                HoraFin = new TimeOnly(12, 0)
            });
        }

        foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
        {
            horarios.Add(new HorarioAtencion
            {
                MedicoId = luis.Id,
                DiaSemana = dia,
                HoraInicio = new TimeOnly(14, 0),
                HoraFin = new TimeOnly(17, 0)
            });
        }

        db.HorariosAtencion.AddRange(horarios);

        var usuarios = new List<Usuario>
        {
            new()
            {
                NombreCompleto = "Administrador del Sistema",
                Correo = "admin@mediturno.do",
                Rol = RolUsuario.Administrador
            },
            new()
            {
                NombreCompleto = "María Jiménez",
                Correo = "recepcion@mediturno.do",
                Rol = RolUsuario.Recepcionista
            },
            new()
            {
                NombreCompleto = "Dra. Carmen Reyes",
                Correo = "carmen.reyes@mediturno.do",
                Rol = RolUsuario.Medico,
                MedicoId = carmen.Id
            },
            new()
            {
                NombreCompleto = "Dr. Luis Peña",
                Correo = "luis.pena@mediturno.do",
                Rol = RolUsuario.Medico,
                MedicoId = luis.Id
            }
        };

        var claves = new[] { "Admin123*", "Recepcion123*", "Medico123*", "Medico123*" };

        for (var i = 0; i < usuarios.Count; i++)
        {
            usuarios[i].PasswordHash = hasher.HashPassword(usuarios[i], claves[i]);
        }

        db.Usuarios.AddRange(usuarios);

        db.Pacientes.AddRange(
            new Paciente
            {
                Cedula = "40212345678",
                Nombre = "Ana",
                Apellido = "Martínez",
                FechaNacimiento = new DateOnly(1992, 4, 15),
                Telefono = "8095551234",
                Correo = "ana.martinez@correo.do",
                FechaRegistro = DateTime.Now
            },
            new Paciente
            {
                Cedula = "00112345678",
                Nombre = "Pedro",
                Apellido = "Gómez",
                FechaNacimiento = new DateOnly(1985, 11, 3),
                Telefono = "8295554321",
                Correo = "pedro.gomez@correo.do",
                FechaRegistro = DateTime.Now
            },
            new Paciente
            {
                Cedula = "40287654321",
                Nombre = "Lucía",
                Apellido = "Fernández",
                FechaNacimiento = new DateOnly(2015, 7, 22),
                Telefono = "8495559876",
                Correo = "lucia.fernandez@correo.do",
                FechaRegistro = DateTime.Now
            });

        await db.SaveChangesAsync();
    }
}
