using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Application.DTOs.Auth;

/// <summary>
/// DTO para solicitud de login
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [MaxLength(100, ErrorMessage = "El nombre de usuario no puede exceder 100 caracteres")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;
} 