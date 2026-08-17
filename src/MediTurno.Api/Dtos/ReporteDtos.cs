namespace MediTurno.Api.Dtos;

public class ReporteCitasResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int Pendientes { get; set; }
    public int Confirmadas { get; set; }
    public int Atendidas { get; set; }
    public int Canceladas { get; set; }
    public int Ausentes { get; set; }
    public int Total { get; set; }
}

public class ReporteMedicoResponse
{
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int Atendidas { get; set; }
    public int Canceladas { get; set; }
    public int Ausentes { get; set; }
    public int Total { get; set; }
    public decimal TasaAusentismo { get; set; }
}
