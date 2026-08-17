namespace MediTurno.Api.Entities;

public class Especialidad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Medico> Medicos { get; set; } = new List<Medico>();
}
