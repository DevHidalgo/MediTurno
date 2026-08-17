using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface IPacienteService
{
    Task<Resultado<PacienteResponse>> CrearAsync(CrearPacienteRequest request);
    Task<Resultado<ListaPaginada<PacienteResponse>>> BuscarAsync(string? busqueda, bool incluirInactivos, int pagina, int tamanoPagina);
    Task<Resultado<PacienteResponse>> ObtenerPorIdAsync(int id);
    Task<Resultado<PacienteResponse>> ActualizarAsync(int id, ActualizarPacienteRequest request);
    Task<Resultado<bool>> DesactivarAsync(int id);
}

public class PacienteService(MediTurnoDbContext db, IRelojSistema reloj) : IPacienteService
{
    public async Task<Resultado<PacienteResponse>> CrearAsync(CrearPacienteRequest request)
    {
        var hoy = DateOnly.FromDateTime(reloj.Ahora);

        if (request.FechaNacimiento > hoy)
        {
            return Resultado<PacienteResponse>.Falla(
                TipoError.Validacion, "La fecha de nacimiento no puede ser futura.");
        }

        var cedula = request.Cedula.Trim();

        if (await db.Pacientes.AnyAsync(p => p.Cedula == cedula))
        {
            return Resultado<PacienteResponse>.Falla(
                TipoError.Conflicto, "Ya existe un paciente con esa cédula.");
        }

        var paciente = new Paciente
        {
            Cedula = cedula,
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            FechaNacimiento = request.FechaNacimiento,
            Telefono = request.Telefono.Trim(),
            Correo = request.Correo.Trim().ToLowerInvariant(),
            Activo = true,
            FechaRegistro = reloj.Ahora
        };

        db.Pacientes.Add(paciente);
        await db.SaveChangesAsync();

        return Resultado<PacienteResponse>.Ok(Mapear(paciente));
    }

    public async Task<Resultado<ListaPaginada<PacienteResponse>>> BuscarAsync(
        string? busqueda, bool incluirInactivos, int pagina, int tamanoPagina)
    {
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1 || tamanoPagina > 100) tamanoPagina = 20;

        var consulta = db.Pacientes.AsQueryable();

        if (!incluirInactivos)
        {
            consulta = consulta.Where(p => p.Activo);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            consulta = consulta.Where(p =>
                p.Cedula.Contains(termino) ||
                p.Nombre.ToLower().Contains(termino) ||
                p.Apellido.ToLower().Contains(termino));
        }

        var total = await consulta.CountAsync();

        var pacientes = await consulta
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return Resultado<ListaPaginada<PacienteResponse>>.Ok(new ListaPaginada<PacienteResponse>
        {
            Items = pacientes.Select(Mapear).ToList(),
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    public async Task<Resultado<PacienteResponse>> ObtenerPorIdAsync(int id)
    {
        var paciente = await db.Pacientes.FindAsync(id);

        return paciente is null
            ? Resultado<PacienteResponse>.Falla(TipoError.NoEncontrado, "El paciente no existe.")
            : Resultado<PacienteResponse>.Ok(Mapear(paciente));
    }

    public async Task<Resultado<PacienteResponse>> ActualizarAsync(int id, ActualizarPacienteRequest request)
    {
        var paciente = await db.Pacientes.FindAsync(id);

        if (paciente is null)
        {
            return Resultado<PacienteResponse>.Falla(TipoError.NoEncontrado, "El paciente no existe.");
        }

        paciente.Telefono = request.Telefono.Trim();
        paciente.Correo = request.Correo.Trim().ToLowerInvariant();

        await db.SaveChangesAsync();

        return Resultado<PacienteResponse>.Ok(Mapear(paciente));
    }

    public async Task<Resultado<bool>> DesactivarAsync(int id)
    {
        var paciente = await db.Pacientes.FindAsync(id);

        if (paciente is null)
        {
            return Resultado<bool>.Falla(TipoError.NoEncontrado, "El paciente no existe.");
        }

        paciente.Activo = false;
        await db.SaveChangesAsync();

        return Resultado<bool>.Ok(true);
    }

    private static PacienteResponse Mapear(Paciente paciente) => new()
    {
        Id = paciente.Id,
        Cedula = paciente.Cedula,
        Nombre = paciente.Nombre,
        Apellido = paciente.Apellido,
        FechaNacimiento = paciente.FechaNacimiento,
        Telefono = paciente.Telefono,
        Correo = paciente.Correo,
        Activo = paciente.Activo,
        FechaRegistro = paciente.FechaRegistro
    };
}
