using FluentValidation;
using MoviesApp.Application.DTOs.Auth;

namespace MoviesApp.Application.Validators;

/// <summary>
/// Validador para LoginRequestDto con validaciones de seguridad mejoradas
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    private static readonly string[] DangerousChars = { "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", "--", "/*", "*/" };

    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .Length(3, 100)
            .WithMessage("El nombre de usuario debe tener entre 3 y 100 caracteres")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos")
            .Must(NotContainDangerousCharacters)
            .WithMessage("El nombre de usuario contiene caracteres no permitidos por seguridad");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8) // Incrementado de 6 a 8 por seguridad
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(200) // Incrementado para permitir contraseñas más seguras
            .WithMessage("La contraseña no puede exceder 200 caracteres");

        // Validación adicional para prevenir bypass de usuario
        RuleFor(x => x)
            .Must(request => request != null && !string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            .WithMessage("Los datos de login son requeridos y deben estar completos")
            .OverridePropertyName("Request");
    }

    private static bool NotContainDangerousCharacters(string? username)
    {
        if (string.IsNullOrEmpty(username))
            return true;

        return !DangerousChars.Any(dangerousChar => 
            username.Contains(dangerousChar, StringComparison.OrdinalIgnoreCase));
    }
} 