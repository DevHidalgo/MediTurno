using System.ComponentModel.DataAnnotations;

namespace MediTurno.Api.Dtos;

public class RegistrarAtencionRequest
{
    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(300, MinimumLength = 3)]
    public string Motivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "El diagnóstico es obligatorio.")]
    public string Diagnostico { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Observaciones { get; set; }
}

public class AtencionResponse
{
    public int Id { get; set; }
    public int CitaId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class HistorialItemResponse
{
    public int CitaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}
