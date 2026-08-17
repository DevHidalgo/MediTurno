namespace MediTurno.Api.Entities;

public class Cita
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public Paciente? Paciente { get; set; }
    public int MedicoId { get; set; }
    public Medico? Medico { get; set; }
    public DateTime FechaHora { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Pendiente;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? MotivoCancelacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public Atencion? Atencion { get; set; }

    public bool EstaVigente => Estado == EstadoCita.Pendiente || Estado == EstadoCita.Confirmada;
}
