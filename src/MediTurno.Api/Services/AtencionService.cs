using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface IAtencionService
{
    Task<Resultado<AtencionResponse>> RegistrarAsync(
        int citaId, RegistrarAtencionRequest request, int? medicoIdUsuario, bool esAdministrador);

    Task<Resultado<IReadOnlyList<HistorialItemResponse>>> ObtenerHistorialAsync(int pacienteId);
}

public class AtencionService(MediTurnoDbContext db, IRelojSistema reloj) : IAtencionService
{
    public async Task<Resultado<AtencionResponse>> RegistrarAsync(
        int citaId, RegistrarAtencionRequest request, int? medicoIdUsuario, bool esAdministrador)
    {
        var cita = await db.Citas
            .Include(c => c.Atencion)
            .FirstOrDefaultAsync(c => c.Id == citaId);

        if (cita is null)
        {
            return Resultado<AtencionResponse>.Falla(TipoError.NoEncontrado, "La cita no existe.");
        }

        if (!esAdministrador && cita.MedicoId != medicoIdUsuario)
        {
            return Resultado<AtencionResponse>.Falla(
                TipoError.Prohibido, "Solo el médico asignado puede registrar la atención de esta cita.");
        }

        if (cita.Atencion is not null)
        {
            return Resultado<AtencionResponse>.Falla(
                TipoError.Conflicto, "La cita ya tiene una atención registrada.");
        }

        if (cita.Estado != EstadoCita.Confirmada)
        {
            return Resultado<AtencionResponse>.Falla(
                TipoError.Conflicto,
                $"Solo se puede registrar la atención de una cita confirmada. Estado actual: {cita.Estado}.");
        }

        var atencion = new Atencion
        {
            CitaId = cita.Id,
            Motivo = request.Motivo.Trim(),
            Diagnostico = request.Diagnostico.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim(),
            FechaRegistro = reloj.Ahora
        };

        db.Atenciones.Add(atencion);
        cita.Estado = EstadoCita.Atendida;

        await db.SaveChangesAsync();

        return Resultado<AtencionResponse>.Ok(new AtencionResponse
        {
            Id = atencion.Id,
            CitaId = atencion.CitaId,
            FechaRegistro = atencion.FechaRegistro,
            Motivo = atencion.Motivo,
            Diagnostico = atencion.Diagnostico,
            Observaciones = atencion.Observaciones
        });
    }

    public async Task<Resultado<IReadOnlyList<HistorialItemResponse>>> ObtenerHistorialAsync(int pacienteId)
    {
        if (!await db.Pacientes.AnyAsync(p => p.Id == pacienteId))
        {
            return Resultado<IReadOnlyList<HistorialItemResponse>>.Falla(
                TipoError.NoEncontrado, "El paciente no existe.");
        }

        var historial = await db.Atenciones
            .Include(a => a.Cita)
                .ThenInclude(c => c!.Medico)
                    .ThenInclude(m => m!.Especialidad)
            .Where(a => a.Cita!.PacienteId == pacienteId)
            .OrderByDescending(a => a.Cita!.FechaHora)
            .ToListAsync();

        IReadOnlyList<HistorialItemResponse> respuesta = historial.Select(a => new HistorialItemResponse
        {
            CitaId = a.CitaId,
            Fecha = a.Cita!.FechaHora,
            Medico = a.Cita.Medico?.NombreCompleto ?? string.Empty,
            Especialidad = a.Cita.Medico?.Especialidad?.Nombre ?? string.Empty,
            Motivo = a.Motivo,
            Diagnostico = a.Diagnostico,
            Observaciones = a.Observaciones
        }).ToList();

        return Resultado<IReadOnlyList<HistorialItemResponse>>.Ok(respuesta);
    }
}
