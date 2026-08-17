using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface IDisponibilidadService
{
    Task<Resultado<IReadOnlyList<BloqueDisponibleResponse>>> ObtenerDisponibilidadAsync(int medicoId, DateOnly fecha);
    IReadOnlyList<DateTime> GenerarBloques(Medico medico, DateOnly fecha);
}

public class DisponibilidadService(MediTurnoDbContext db, IRelojSistema reloj) : IDisponibilidadService
{
    public async Task<Resultado<IReadOnlyList<BloqueDisponibleResponse>>> ObtenerDisponibilidadAsync(
        int medicoId, DateOnly fecha)
    {
        var medico = await db.Medicos
            .Include(m => m.Horarios)
            .FirstOrDefaultAsync(m => m.Id == medicoId);

        if (medico is null)
        {
            return Resultado<IReadOnlyList<BloqueDisponibleResponse>>.Falla(
                TipoError.NoEncontrado, "El médico no existe.");
        }

        var ahora = reloj.Ahora;

        if (fecha < DateOnly.FromDateTime(ahora))
        {
            return Resultado<IReadOnlyList<BloqueDisponibleResponse>>.Falla(
                TipoError.Validacion, "La fecha consultada no puede ser anterior a la fecha actual.");
        }

        var bloques = GenerarBloques(medico, fecha);

        if (bloques.Count == 0)
        {
            return Resultado<IReadOnlyList<BloqueDisponibleResponse>>.Ok([]);
        }

        var inicioDia = fecha.ToDateTime(TimeOnly.MinValue);
        var finDia = inicioDia.AddDays(1);

        var ocupados = await db.Citas
            .Where(c => c.MedicoId == medicoId
                        && c.FechaHora >= inicioDia
                        && c.FechaHora < finDia
                        && (c.Estado == EstadoCita.Pendiente || c.Estado == EstadoCita.Confirmada))
            .Select(c => c.FechaHora)
            .ToListAsync();

        var disponibles = bloques
            .Where(bloque => bloque > ahora && !ocupados.Contains(bloque))
            .Select(bloque => new BloqueDisponibleResponse
            {
                FechaHora = bloque,
                Hora = TimeOnly.FromDateTime(bloque)
            })
            .ToList();

        return Resultado<IReadOnlyList<BloqueDisponibleResponse>>.Ok(disponibles);
    }

    public IReadOnlyList<DateTime> GenerarBloques(Medico medico, DateOnly fecha)
    {
        var bloques = new List<DateTime>();

        if (medico.DuracionConsultaMinutos <= 0)
        {
            return bloques;
        }

        var horariosDelDia = medico.Horarios
            .Where(h => h.DiaSemana == fecha.DayOfWeek)
            .OrderBy(h => h.HoraInicio);

        foreach (var horario in horariosDelDia)
        {
            var inicio = horario.HoraInicio;

            while (true)
            {
                var fin = inicio.AddMinutes(medico.DuracionConsultaMinutos);

                if (fin <= inicio || fin > horario.HoraFin)
                {
                    break;
                }

                bloques.Add(fecha.ToDateTime(inicio));
                inicio = fin;
            }
        }

        return bloques.OrderBy(b => b).ToList();
    }
}
