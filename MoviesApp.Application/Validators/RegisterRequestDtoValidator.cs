using FluentValidation;
using MoviesApp.Application.DTOs.Auth;

namespace MoviesApp.Application.Validators;

/// <summary>
/// Validador para RegisterRequestDto con validaciones de seguridad mejoradas
/// </summary>
public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    private static readonly string[] DangerousChars = { "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", "--", "/*", "*/" };

    public RegisterRequestDtoValidator()
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

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres")
            .Must(NotContainDangerousCharacters)
            .WithMessage("El email contiene caracteres no permitidos por seguridad");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(200)
            .WithMessage("La contraseña no puede exceder 200 caracteres")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)")
            .WithMessage("La contraseña debe contener al menos una letra minúscula, una mayúscula y un número");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.Password)
            .WithMessage("Las contraseñas no coinciden");

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres")
            .Must(NotContainDangerousCharacters)
            .WithMessage("El nombre contiene caracteres no permitidos por seguridad")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("El apellido no puede exceder 100 caracteres")
            .Must(NotContainDangerousCharacters)
            .WithMessage("El apellido contiene caracteres no permitidos por seguridad")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x)
            .Must(request => request != null && 
                             !string.IsNullOrWhiteSpace(request.Username) && 
                             !string.IsNullOrWhiteSpace(request.Password) &&
                             !string.IsNullOrWhiteSpace(request.Email))
            .WithMessage("Los datos de registro son requeridos y deben estar completos")
            .OverridePropertyName("Request");
    }

    private static bool NotContainDangerousCharacters(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        return !DangerousChars.Any(dangerousChar => 
            input.Contains(dangerousChar, StringComparison.OrdinalIgnoreCase));
    }
} 