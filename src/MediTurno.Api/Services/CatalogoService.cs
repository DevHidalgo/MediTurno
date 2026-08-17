using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface ICatalogoService
{
    Task<Resultado<EspecialidadResponse>> CrearEspecialidadAsync(CrearEspecialidadRequest request);
    Task<Resultado<IReadOnlyList<EspecialidadResponse>>> ListarEspecialidadesAsync();
    Task<Resultado<bool>> EliminarEspecialidadAsync(int id);
    Task<Resultado<MedicoResponse>> CrearMedicoAsync(CrearMedicoRequest request);
    Task<Resultado<IReadOnlyList<MedicoResponse>>> ListarMedicosAsync();
    Task<Resultado<MedicoResponse>> ObtenerMedicoAsync(int id);
    Task<Resultado<IReadOnlyList<HorarioResponse>>> DefinirHorariosAsync(int medicoId, DefinirHorariosRequest request);
    Task<Resultado<IReadOnlyList<HorarioResponse>>> ObtenerHorariosAsync(int medicoId);
}

public class CatalogoService(MediTurnoDbContext db) : ICatalogoService
{
    private static readonly string[] NombresDias =
        ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    public async Task<Resultado<EspecialidadResponse>> CrearEspecialidadAsync(CrearEspecialidadRequest request)
    {
        var nombre = request.Nombre.Trim();

        if (await db.Especialidades.AnyAsync(e => e.Nombre == nombre))
        {
            return Resultado<EspecialidadResponse>.Falla(
                TipoError.Conflicto, "Ya existe una especialidad con ese nombre.");
        }

        var especialidad = new Especialidad { Nombre = nombre };
        db.Especialidades.Add(especialidad);
        await db.SaveChangesAsync();

        return Resultado<EspecialidadResponse>.Ok(
            new EspecialidadResponse { Id = especialidad.Id, Nombre = especialidad.Nombre });
    }

    public async Task<Resultado<IReadOnlyList<EspecialidadResponse>>> ListarEspecialidadesAsync()
    {
        var especialidades = await db.Especialidades
            .OrderBy(e => e.Nombre)
            .Select(e => new EspecialidadResponse { Id = e.Id, Nombre = e.Nombre })
            .ToListAsync();

        return Resultado<IReadOnlyList<EspecialidadResponse>>.Ok(especialidades);
    }

    public async Task<Resultado<bool>> EliminarEspecialidadAsync(int id)
    {
        var especialidad = await db.Especialidades.FindAsync(id);

        if (especialidad is null)
        {
            return Resultado<bool>.Falla(TipoError.NoEncontrado, "La especialidad no existe.");
        }

        if (await db.Medicos.AnyAsync(m => m.EspecialidadId == id))
        {
            return Resultado<bool>.Falla(
                TipoError.Conflicto, "No se puede eliminar una especialidad con médicos asociados.");
        }

        db.Especialidades.Remove(especialidad);
        await db.SaveChangesAsync();

        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<MedicoResponse>> CrearMedicoAsync(CrearMedicoRequest request)
    {
        var especialidad = await db.Especialidades.FindAsync(request.EspecialidadId);

        if (especialidad is null)
        {
            return Resultado<MedicoResponse>.Falla(TipoError.Validacion, "La especialidad indicada no existe.");
        }

        var exequatur = request.Exequatur.Trim();

        if (await db.Medicos.AnyAsync(m => m.Exequatur == exequatur))
        {
            return Resultado<MedicoResponse>.Falla(TipoError.Conflicto, "Ya existe un médico con ese exequátur.");
        }

        var medico = new Medico
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Exequatur = exequatur,
            EspecialidadId = request.EspecialidadId,
            DuracionConsultaMinutos = request.DuracionConsultaMinutos,
            Activo = true
        };

        db.Medicos.Add(medico);
        await db.SaveChangesAsync();

        medico.Especialidad = especialidad;
        return Resultado<MedicoResponse>.Ok(Mapear(medico));
    }

    public async Task<Resultado<IReadOnlyList<MedicoResponse>>> ListarMedicosAsync()
    {
        var medicos = await db.Medicos
            .Include(m => m.Especialidad)
            .OrderBy(m => m.NombreCompleto)
            .ToListAsync();

        IReadOnlyList<MedicoResponse> respuesta = medicos.Select(Mapear).ToList();
        return Resultado<IReadOnlyList<MedicoResponse>>.Ok(respuesta);
    }

    public async Task<Resultado<MedicoResponse>> ObtenerMedicoAsync(int id)
    {
        var medico = await db.Medicos
            .Include(m => m.Especialidad)
            .FirstOrDefaultAsync(m => m.Id == id);

        return medico is null
            ? Resultado<MedicoResponse>.Falla(TipoError.NoEncontrado, "El médico no existe.")
            : Resultado<MedicoResponse>.Ok(Mapear(medico));
    }

    public async Task<Resultado<IReadOnlyList<HorarioResponse>>> DefinirHorariosAsync(
        int medicoId, DefinirHorariosRequest request)
    {
        var medico = await db.Medicos
            .Include(m => m.Horarios)
            .FirstOrDefaultAsync(m => m.Id == medicoId);

        if (medico is null)
        {
            return Resultado<IReadOnlyList<HorarioResponse>>.Falla(TipoError.NoEncontrado, "El médico no existe.");
        }

        foreach (var horario in request.Horarios)
        {
            if (horario.DiaSemana is < 0 or > 6)
            {
                return Resultado<IReadOnlyList<HorarioResponse>>.Falla(
                    TipoError.Validacion, "El día de la semana debe estar entre 0 (domingo) y 6 (sábado).");
            }

            if (horario.HoraFin <= horario.HoraInicio)
            {
                return Resultado<IReadOnlyList<HorarioResponse>>.Falla(
                    TipoError.Validacion,
                    $"En {NombresDias[horario.DiaSemana]} la hora de fin debe ser posterior a la hora de inicio.");
            }
        }

        var solapado = request.Horarios
            .GroupBy(h => h.DiaSemana)
            .Select(grupo => grupo.OrderBy(h => h.HoraInicio).ToList())
            .Where(dia => dia.Zip(dia.Skip(1), (actual, siguiente) => siguiente.HoraInicio < actual.HoraFin).Any(x => x))
            .Select(dia => dia[0].DiaSemana)
            .FirstOrDefault(-1);

        if (solapado >= 0)
        {
            return Resultado<IReadOnlyList<HorarioResponse>>.Falla(
                TipoError.Validacion,
                $"Los rangos horarios de {NombresDias[solapado]} se solapan entre sí.");
        }

        db.HorariosAtencion.RemoveRange(medico.Horarios);

        var nuevos = request.Horarios.Select(h => new HorarioAtencion
        {
            MedicoId = medicoId,
            DiaSemana = (DayOfWeek)h.DiaSemana,
            HoraInicio = h.HoraInicio,
            HoraFin = h.HoraFin
        }).ToList();

        db.HorariosAtencion.AddRange(nuevos);
        await db.SaveChangesAsync();

        return Resultado<IReadOnlyList<HorarioResponse>>.Ok(nuevos.Select(MapearHorario).ToList());
    }

    public async Task<Resultado<IReadOnlyList<HorarioResponse>>> ObtenerHorariosAsync(int medicoId)
    {
        if (!await db.Medicos.AnyAsync(m => m.Id == medicoId))
        {
            return Resultado<IReadOnlyList<HorarioResponse>>.Falla(TipoError.NoEncontrado, "El médico no existe.");
        }

        var horarios = await db.HorariosAtencion
            .Where(h => h.MedicoId == medicoId)
            .OrderBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .ToListAsync();

        IReadOnlyList<HorarioResponse> respuesta = horarios.Select(MapearHorario).ToList();
        return Resultado<IReadOnlyList<HorarioResponse>>.Ok(respuesta);
    }

    private static MedicoResponse Mapear(Medico medico) => new()
    {
        Id = medico.Id,
        NombreCompleto = medico.NombreCompleto,
        Exequatur = medico.Exequatur,
        EspecialidadId = medico.EspecialidadId,
        Especialidad = medico.Especialidad?.Nombre ?? string.Empty,
        DuracionConsultaMinutos = medico.DuracionConsultaMinutos,
        Activo = medico.Activo
    };

    private static HorarioResponse MapearHorario(HorarioAtencion horario) => new()
    {
        DiaSemana = (int)horario.DiaSemana,
        Dia = NombresDias[(int)horario.DiaSemana],
        HoraInicio = horario.HoraInicio,
        HoraFin = horario.HoraFin
    };
}
