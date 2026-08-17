using System.Security.Claims;
using System.Text;
using MediTurno.Api.Common;
using MediTurno.Api.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MediTurno.Api.Services;

public interface ITokenService
{
    (string Token, DateTime Expira) Generar(Usuario usuario);
}

public class TokenService(IOptions<JwtOptions> opciones, IRelojSistema reloj) : ITokenService
{
    private readonly JwtOptions _opciones = opciones.Value;

    public (string Token, DateTime Expira) Generar(Usuario usuario)
    {
        var expira = reloj.Ahora.AddMinutes(_opciones.MinutosVigencia);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("role", usuario.Rol.ToString()),
            new("nombre", usuario.NombreCompleto)
        };

        if (usuario.MedicoId.HasValue)
        {
            claims.Add(new Claim("medicoId", usuario.MedicoId.Value.ToString()));
        }

        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Issuer,
            Audience = _opciones.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expira.ToUniversalTime(),
            SigningCredentials = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expira);
    }
}
