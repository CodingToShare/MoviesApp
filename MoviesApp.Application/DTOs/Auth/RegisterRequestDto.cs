using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Application.DTOs.Auth;

/// <summary>
/// DTO para registro de nuevos usuarios
/// </summary>
public class RegisterRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre de usuario no puede exceder 100 caracteres")]
    [MinLength(3, ErrorMessage = "El nombre de usuario debe tener al menos 3 caracteres")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    [MaxLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string? LastName { get; set; }
} 