using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface IReporteService
{
    Task<Resultado<ReporteCitasResponse>> CitasPorEstadoAsync(DateOnly desde, DateOnly hasta);
    Task<Resultado<ReporteMedicoResponse>> PorMedicoAsync(int medicoId, DateOnly desde, DateOnly hasta);
}

public class ReporteService(MediTurnoDbContext db) : IReporteService
{
    public const int RangoMaximoDias = 365;

    public async Task<Resultado<ReporteCitasResponse>> CitasPorEstadoAsync(DateOnly desde, DateOnly hasta)
    {
        var validacion = ValidarRango(desde, hasta);

        if (validacion is not null)
        {
            return Resultado<ReporteCitasResponse>.Falla(TipoError.Validacion, validacion);
        }

        var citas = await ConsultarPorRango(desde, hasta).ToListAsync();

        return Resultado<ReporteCitasResponse>.Ok(new ReporteCitasResponse
        {
            Desde = desde,
            Hasta = hasta,
            Pendientes = citas.Count(c => c.Estado == EstadoCita.Pendiente),
            Confirmadas = citas.Count(c => c.Estado == EstadoCita.Confirmada),
            Atendidas = citas.Count(c => c.Estado == EstadoCita.Atendida),
            Canceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada),
            Ausentes = citas.Count(c => c.Estado == EstadoCita.Ausente),
            Total = citas.Count
        });
    }

    public async Task<Resultado<ReporteMedicoResponse>> PorMedicoAsync(int medicoId, DateOnly desde, DateOnly hasta)
    {
        var validacion = ValidarRango(desde, hasta);

        if (validacion is not null)
        {
            return Resultado<ReporteMedicoResponse>.Falla(TipoError.Validacion, validacion);
        }

        var medico = await db.Medicos.FindAsync(medicoId);

        if (medico is null)
        {
            return Resultado<ReporteMedicoResponse>.Falla(TipoError.NoEncontrado, "El médico no existe.");
        }

        var citas = await ConsultarPorRango(desde, hasta)
            .Where(c => c.MedicoId == medicoId)
            .ToListAsync();

        var atendidas = citas.Count(c => c.Estado == EstadoCita.Atendida);
        var canceladas = citas.Count(c => c.Estado == EstadoCita.Cancelada);
        var ausentes = citas.Count(c => c.Estado == EstadoCita.Ausente);
        var cerradas = atendidas + ausentes;

        return Resultado<ReporteMedicoResponse>.Ok(new ReporteMedicoResponse
        {
            MedicoId = medico.Id,
            Medico = medico.NombreCompleto,
            Desde = desde,
            Hasta = hasta,
            Atendidas = atendidas,
            Canceladas = canceladas,
            Ausentes = ausentes,
            Total = citas.Count,
            TasaAusentismo = cerradas == 0 ? 0 : Math.Round((decimal)ausentes / cerradas * 100, 2)
        });
    }

    private IQueryable<Cita> ConsultarPorRango(DateOnly desde, DateOnly hasta)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return db.Citas.Where(c => c.FechaHora >= inicio && c.FechaHora < fin);
    }

    private static string? ValidarRango(DateOnly desde, DateOnly hasta)
    {
        if (desde > hasta)
        {
            return "La fecha de inicio no puede ser posterior a la fecha de fin.";
        }

        if (hasta.DayNumber - desde.DayNumber > RangoMaximoDias)
        {
            return $"El rango consultado no puede exceder {RangoMaximoDias} días.";
        }

        return null;
    }
}
