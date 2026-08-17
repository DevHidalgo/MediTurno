using MediTurno.Api.Common;
using MediTurno.Api.Data;
using MediTurno.Api.Dtos;
using MediTurno.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Services;

public interface IAuthService
{
    Task<Resultado<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Resultado<UsuarioResponse>> CrearUsuarioAsync(CrearUsuarioRequest request);
    Task<Resultado<IReadOnlyList<UsuarioResponse>>> ListarUsuariosAsync();
}

public class AuthService(
    MediTurnoDbContext db,
    ITokenService tokenService,
    IPasswordHasher<Usuario> hasher) : IAuthService
{
    private const string MensajeCredencialesInvalidas = "Credenciales inválidas.";

    public async Task<Resultado<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var usuario = await db.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == request.Correo);

        if (usuario is null)
        {
            return Resultado<LoginResponse>.Falla(TipoError.NoAutorizado, MensajeCredencialesInvalidas);
        }

        var verificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);

        if (verificacion == PasswordVerificationResult.Failed)
        {
            return Resultado<LoginResponse>.Falla(TipoError.NoAutorizado, MensajeCredencialesInvalidas);
        }

        if (!usuario.Activo)
        {
            return Resultado<LoginResponse>.Falla(TipoError.Prohibido, "El usuario se encuentra desactivado.");
        }

        var (token, expira) = tokenService.Generar(usuario);

        return Resultado<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            Expira = expira,
            Rol = usuario.Rol.ToString(),
            NombreCompleto = usuario.NombreCompleto
        });
    }

    public async Task<Resultado<UsuarioResponse>> CrearUsuarioAsync(CrearUsuarioRequest request)
    {
        if (!Enum.TryParse<RolUsuario>(request.Rol, ignoreCase: true, out var rol))
        {
            return Resultado<UsuarioResponse>.Falla(
                TipoError.Validacion,
                "El rol indicado no es válido. Valores permitidos: Administrador, Medico, Recepcionista.");
        }

        var correoNormalizado = request.Correo.Trim().ToLowerInvariant();

        if (await db.Usuarios.AnyAsync(u => u.Correo == correoNormalizado))
        {
            return Resultado<UsuarioResponse>.Falla(TipoError.Conflicto, "Ya existe un usuario con ese correo.");
        }

        if (rol == RolUsuario.Medico)
        {
            if (!request.MedicoId.HasValue)
            {
                return Resultado<UsuarioResponse>.Falla(
                    TipoError.Validacion, "Un usuario con rol Medico debe estar asociado a un médico.");
            }

            if (!await db.Medicos.AnyAsync(m => m.Id == request.MedicoId.Value))
            {
                return Resultado<UsuarioResponse>.Falla(TipoError.Validacion, "El médico indicado no existe.");
            }
        }

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correoNormalizado,
            Rol = rol,
            Activo = true,
            MedicoId = rol == RolUsuario.Medico ? request.MedicoId : null
        };

        usuario.PasswordHash = hasher.HashPassword(usuario, request.Password);

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return Resultado<UsuarioResponse>.Ok(Mapear(usuario));
    }

    public async Task<Resultado<IReadOnlyList<UsuarioResponse>>> ListarUsuariosAsync()
    {
        var usuarios = await db.Usuarios
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync();

        IReadOnlyList<UsuarioResponse> respuesta = usuarios.Select(Mapear).ToList();
        return Resultado<IReadOnlyList<UsuarioResponse>>.Ok(respuesta);
    }

    private static UsuarioResponse Mapear(Usuario usuario) => new()
    {
        Id = usuario.Id,
        NombreCompleto = usuario.NombreCompleto,
        Correo = usuario.Correo,
        Rol = usuario.Rol.ToString(),
        Activo = usuario.Activo,
        MedicoId = usuario.MedicoId
    };
}
