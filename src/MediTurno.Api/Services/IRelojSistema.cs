namespace MediTurno.Api.Services;

public interface IRelojSistema
{
    DateTime Ahora { get; }
}

public class RelojSistema : IRelojSistema
{
    public DateTime Ahora => DateTime.Now;
}
