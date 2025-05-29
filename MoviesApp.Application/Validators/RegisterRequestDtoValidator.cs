using FluentValidation;
using MoviesApp.Application.DTOs.Auth;

namespace MoviesApp.Application.Validators;

/// <summary>
/// Validador para RegisterRequestDto
/// </summary>
public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .Length(3, 100)
            .WithMessage("El nombre de usuario debe tener entre 3 y 100 caracteres")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("El nombre de usuario solo puede contener letras, números, puntos, guiones y guiones bajos");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email no es válido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(6)
            .WithMessage("La contraseña debe tener al menos 6 caracteres")
            .MaximumLength(100)
            .WithMessage("La contraseña no puede exceder 100 caracteres")
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
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("El apellido no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.LastName));
    }
} 