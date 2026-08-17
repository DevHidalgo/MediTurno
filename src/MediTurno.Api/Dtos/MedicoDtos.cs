using System.ComponentModel.DataAnnotations;

namespace MediTurno.Api.Dtos;

public class CrearEspecialidadRequest
{
    [Required(ErrorMessage = "El nombre de la especialidad es obligatorio.")]
    [StringLength(100, MinimumLength = 3)]
    public string Nombre { get; set; } = string.Empty;
}

public class EspecialidadResponse
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CrearMedicoRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El exequátur es obligatorio.")]
    [StringLength(50, MinimumLength = 3)]
    public string Exequatur { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especialidad es obligatoria.")]
    public int EspecialidadId { get; set; }

    [Range(10, 120, ErrorMessage = "La duración de la consulta debe estar entre 10 y 120 minutos.")]
    public int DuracionConsultaMinutos { get; set; }
}

public class MedicoResponse
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Exequatur { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public int DuracionConsultaMinutos { get; set; }
    public bool Activo { get; set; }
}

public class HorarioRequest
{
    [Range(0, 6, ErrorMessage = "El día de la semana debe estar entre 0 (domingo) y 6 (sábado).")]
    public int DiaSemana { get; set; }

    [Required]
    public TimeOnly HoraInicio { get; set; }

    [Required]
    public TimeOnly HoraFin { get; set; }
}

public class DefinirHorariosRequest
{
    [Required]
    public List<HorarioRequest> Horarios { get; set; } = [];
}

public class HorarioResponse
{
    public int DiaSemana { get; set; }
    public string Dia { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
}

public class BloqueDisponibleResponse
{
    public DateTime FechaHora { get; set; }
    public TimeOnly Hora { get; set; }
}
