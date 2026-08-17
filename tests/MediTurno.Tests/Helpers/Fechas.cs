namespace MediTurno.Tests.Helpers;

public static class Fechas
{
    public static DateTime ProximoDiaHabil(TimeOnly hora, int diasExtra = 0)
    {
        var fecha = DateTime.Now.Date.AddDays(7 + diasExtra);

        while (fecha.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            fecha = fecha.AddDays(1);
        }

        return fecha.Add(hora.ToTimeSpan());
    }
}
