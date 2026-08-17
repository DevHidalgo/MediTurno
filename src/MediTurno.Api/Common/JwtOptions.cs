namespace MediTurno.Api.Common;

public class JwtOptions
{
    public const string SeccionConfiguracion = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int MinutosVigencia { get; set; } = 60;
}
