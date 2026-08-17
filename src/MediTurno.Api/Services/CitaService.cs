using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface ICitaService
{
    Task<Resultado<CitaResponse>> ReservarAsync(CrearCitaRequest request);
    Task<Resultado<CitaResponse>> ReprogramarAsync(int id, ReprogramarCitaRequest request);
    Task<Resultado<CitaResponse>> CancelarAsync(int id, CancelarCitaRequest request);
    Task<Resultado<CitaResponse>> ConfirmarAsync(int id);
    Task<Resultado<CitaResponse>> ObtenerPorIdAsync(int id);
    Task<Resultado<IReadOnlyList<CitaResponse>>> ListarAsync(int? medicoId, int? pacienteId, DateOnly? fecha);
    Task<Resultado<int>> MarcarAusentesAsync();
}

public class CitaService(
    MediTurnoDbContext db,
    IDisponibilidadService disponibilidad,
    IRelojSistema reloj) : ICitaService
{
    public const int AntelacionMinimaHoras = 2;

    private const string MensajeHorarioOcupado = "El horario seleccionado ya no está disponible.";

    public async Task<Resultado<CitaResponse>> ReservarAsync(CrearCitaRequest request)
    {
        var paciente = await db.Pacientes.FindAsync(request.PacienteId);

        if (paciente is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "El paciente indicado no existe.");
        }

        if (!paciente.Activo)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto, "No se pueden agendar citas para un paciente inactivo.");
        }

        var medico = await db.Medicos
            .Include(m => m.Horarios)
            .FirstOrDefaultAsync(m => m.Id == request.MedicoId);

        if (medico is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "El médico indicado no existe.");
        }

        if (!medico.Activo)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto, "El médico indicado se encuentra inactivo.");
        }

        var validacion = await ValidarHorarioAsync(medico, request.FechaHora, null);

        if (!validacion.Exitoso)
        {
            return Resultado<CitaResponse>.Falla(validacion.Error, validacion.Mensaje);
        }

        var pacienteOcupado = await db.Citas.AnyAsync(c =>
            c.PacienteId == request.PacienteId &&
            c.FechaHora == request.FechaHora &&
            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada));

        if (pacienteOcupado)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto, "El paciente ya tiene una cita vigente en ese horario.");
        }

        var cita = new Cita
        {
            PacienteId = request.PacienteId,
            MedicoId = request.MedicoId,
            FechaHora = request.FechaHora,
            Estado = EstadoCita.Pendiente,
            MotivoConsulta = request.MotivoConsulta.Trim(),
            FechaCreacion = reloj.Ahora
        };

        db.Citas.Add(cita);
        await db.SaveChangesAsync();

        return await DevolverAsync(cita.Id);
    }

    public async Task<Resultado<CitaResponse>> ReprogramarAsync(int id, ReprogramarCitaRequest request)
    {
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.Id == id);

        if (cita is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "La cita no existe.");
        }

        if (!cita.EstaVigente)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto,
                $"No se puede reprogramar una cita en estado {cita.Estado}.");
        }

        var medico = await db.Medicos
            .Include(m => m.Horarios)
            .FirstOrDefaultAsync(m => m.Id == cita.MedicoId);

        if (medico is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "El médico de la cita no existe.");
        }

        var validacion = await ValidarHorarioAsync(medico, request.NuevaFechaHora, cita.Id);

        if (!validacion.Exitoso)
        {
            return Resultado<CitaResponse>.Falla(validacion.Error, validacion.Mensaje);
        }

        var pacienteOcupado = await db.Citas.AnyAsync(c =>
            c.PacienteId == cita.PacienteId &&
            c.Id != cita.Id &&
            c.FechaHora == request.NuevaFechaHora &&
            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada));

        if (pacienteOcupado)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto, "El paciente ya tiene una cita vigente en ese horario.");
        }

        cita.FechaHora = request.NuevaFechaHora;
        await db.SaveChangesAsync();

        return await DevolverAsync(cita.Id);
    }

    public async Task<Resultado<CitaResponse>> CancelarAsync(int id, CancelarCitaRequest request)
    {
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.Id == id);

        if (cita is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "La cita no existe.");
        }

        if (!cita.EstaVigente)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto,
                $"No se puede cancelar una cita en estado {cita.Estado}.");
        }

        cita.Estado = EstadoCita.Cancelada;
        cita.MotivoCancelacion = request.Motivo.Trim();
        cita.FechaCancelacion = reloj.Ahora;

        await db.SaveChangesAsync();

        return await DevolverAsync(cita.Id);
    }

    public async Task<Resultado<CitaResponse>> ConfirmarAsync(int id)
    {
        var cita = await db.Citas.FirstOrDefaultAsync(c => c.Id == id);

        if (cita is null)
        {
            return Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "La cita no existe.");
        }

        if (cita.Estado != EstadoCita.Pendiente)
        {
            return Resultado<CitaResponse>.Falla(
                TipoError.Conflicto,
                $"Solo se puede confirmar una cita pendiente. Estado actual: {cita.Estado}.");
        }

        cita.Estado = EstadoCita.Confirmada;
        cita.FechaConfirmacion = reloj.Ahora;

        await db.SaveChangesAsync();

        return await DevolverAsync(cita.Id);
    }

    public async Task<Resultado<CitaResponse>> ObtenerPorIdAsync(int id)
    {
        var cita = await ConsultaCompleta().FirstOrDefaultAsync(c => c.Id == id);

        return cita is null
            ? Resultado<CitaResponse>.Falla(TipoError.NoEncontrado, "La cita no existe.")
            : Resultado<CitaResponse>.Ok(Mapear(cita));
    }

    public async Task<Resultado<IReadOnlyList<CitaResponse>>> ListarAsync(
        int? medicoId, int? pacienteId, DateOnly? fecha)
    {
        var consulta = ConsultaCompleta();

        if (medicoId.HasValue)
        {
            consulta = consulta.Where(c => c.MedicoId == medicoId.Value);
        }

        if (pacienteId.HasValue)
        {
            consulta = consulta.Where(c => c.PacienteId == pacienteId.Value);
        }

        if (fecha.HasValue)
        {
            var inicio = fecha.Value.ToDateTime(TimeOnly.MinValue);
            var fin = inicio.AddDays(1);
            consulta = consulta.Where(c => c.FechaHora >= inicio && c.FechaHora < fin);
        }

        var citas = await consulta.OrderBy(c => c.FechaHora).ToListAsync();

        IReadOnlyList<CitaResponse> respuesta = citas.Select(Mapear).ToList();
        return Resultado<IReadOnlyList<CitaResponse>>.Ok(respuesta);
    }

    public async Task<Resultado<int>> MarcarAusentesAsync()
    {
        var ahora = reloj.Ahora;

        var pendientes = await db.Citas
            .Where(c => c.Estado == EstadoCita.Confirmada && c.FechaHora < ahora)
            .ToListAsync();

        foreach (var cita in pendientes)
        {
            cita.Estado = EstadoCita.Ausente;
        }

        await db.SaveChangesAsync();

        return Resultado<int>.Ok(pendientes.Count);
    }

    private async Task<Resultado<bool>> ValidarHorarioAsync(Medico medico, DateTime fechaHora, int? citaExcluida)
    {
        var ahora = reloj.Ahora;

        if (fechaHora <= ahora)
        {
            return Resultado<bool>.Falla(TipoError.Validacion, "La fecha y hora de la cita deben ser futuras.");
        }

        if (fechaHora < ahora.AddHours(AntelacionMinimaHoras))
        {
            return Resultado<bool>.Falla(
                TipoError.Validacion,
                $"La cita debe reservarse con al menos {AntelacionMinimaHoras} horas de antelación.");
        }

        var bloques = disponibilidad.GenerarBloques(medico, DateOnly.FromDateTime(fechaHora));

        if (!bloques.Contains(fechaHora))
        {
            return Resultado<bool>.Falla(
                TipoError.Validacion,
                "El horario solicitado no corresponde a un bloque de atención del médico.");
        }

        var ocupado = await db.Citas.AnyAsync(c =>
            c.MedicoId == medico.Id &&
            c.FechaHora == fechaHora &&
            (citaExcluida == null || c.Id != citaExcluida) &&
            (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada));

        return ocupado
            ? Resultado<bool>.Falla(TipoError.Conflicto, MensajeHorarioOcupado)
            : Resultado<bool>.Ok(true);
    }

    private IQueryable<Cita> ConsultaCompleta() =>
        db.Citas
            .Include(c => c.Paciente)
            .Include(c => c.Medico)
                .ThenInclude(m => m!.Especialidad);

    private async Task<Resultado<CitaResponse>> DevolverAsync(int citaId)
    {
        var cita = await ConsultaCompleta().FirstAsync(c => c.Id == citaId);
        return Resultado<CitaResponse>.Ok(Mapear(cita));
    }

    private static CitaResponse Mapear(Cita cita) => new()
    {
        Id = cita.Id,
        PacienteId = cita.PacienteId,
        Paciente = cita.Paciente is null ? string.Empty : $"{cita.Paciente.Nombre} {cita.Paciente.Apellido}",
        MedicoId = cita.MedicoId,
        Medico = cita.Medico?.NombreCompleto ?? string.Empty,
        Especialidad = cita.Medico?.Especialidad?.Nombre ?? string.Empty,
        FechaHora = cita.FechaHora,
        Estado = cita.Estado.ToString(),
        MotivoConsulta = cita.MotivoConsulta,
        MotivoCancelacion = cita.MotivoCancelacion,
        FechaConfirmacion = cita.FechaConfirmacion,
        FechaCancelacion = cita.FechaCancelacion
    };
}
