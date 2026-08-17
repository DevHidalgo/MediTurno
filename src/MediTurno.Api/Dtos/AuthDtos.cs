using System.ComponentModel.DataAnnotations;

namespace MediTurno.Api.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}

public class CrearUsuarioRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150, MinimumLength = 3)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Rol { get; set; } = string.Empty;

    public int? MedicoId { get; set; }
}

public class UsuarioResponse
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int? MedicoId { get; set; }
}
