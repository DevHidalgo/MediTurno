namespace MediTurno.Api.Entities;

public class Medico
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Exequatur { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public Especialidad? Especialidad { get; set; }
    public int DuracionConsultaMinutos { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<HorarioAtencion> Horarios { get; set; } = new List<HorarioAtencion>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
