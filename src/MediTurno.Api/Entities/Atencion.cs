namespace MediTurno.Api.Entities;

public class Atencion
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public Cita? Cita { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }
}
