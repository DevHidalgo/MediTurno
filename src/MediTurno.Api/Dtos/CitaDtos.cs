using System.ComponentModel.DataAnnotations;

namespace MediTurno.Api.Dtos;

public class CrearCitaRequest
{
    [Required(ErrorMessage = "El paciente es obligatorio.")]
    public int PacienteId { get; set; }

    [Required(ErrorMessage = "El médico es obligatorio.")]
    public int MedicoId { get; set; }

    [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
    public DateTime FechaHora { get; set; }

    [Required(ErrorMessage = "El motivo de la consulta es obligatorio.")]
    [StringLength(300, MinimumLength = 3)]
    public string MotivoConsulta { get; set; } = string.Empty;
}

public class ReprogramarCitaRequest
{
    [Required(ErrorMessage = "La nueva fecha y hora son obligatorias.")]
    public DateTime NuevaFechaHora { get; set; }
}

public class CancelarCitaRequest
{
    [Required(ErrorMessage = "El motivo de cancelación es obligatorio.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "El motivo de cancelación es obligatorio.")]
    public string Motivo { get; set; } = string.Empty;
}

public class CitaResponse
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string? MotivoCancelacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
}
