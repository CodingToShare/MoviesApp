using FluentValidation;
using MoviesApp.Application.DTOs;

namespace MoviesApp.Application.Validators;

/// <summary>
/// Validador para el DTO de actualización de películas
/// </summary>
public class UpdateMovieDtoValidator : AbstractValidator<UpdateMovieDto>
{
    public UpdateMovieDtoValidator()
    {
        // Validación del nombre de la película (opcional)
        RuleFor(x => x.Film)
            .Length(1, 255)
            .WithMessage("El nombre debe tener entre 1 y 255 caracteres")
            .Must(BeValidFilmName)
            .WithMessage("El nombre de la película contiene caracteres no válidos")
            .When(x => !string.IsNullOrEmpty(x.Film));

        // Validación del género (opcional)
        RuleFor(x => x.Genre)
            .Length(1, 100)
            .WithMessage("El género debe tener entre 1 y 100 caracteres")
            .Must(BeValidGenre)
            .WithMessage("El género contiene caracteres no válidos o no es válido")
            .When(x => !string.IsNullOrEmpty(x.Genre));

        // Validación del estudio (opcional)
        RuleFor(x => x.Studio)
            .Length(1, 150)
            .WithMessage("El estudio debe tener entre 1 y 150 caracteres")
            .Must(BeValidStudioName)
            .WithMessage("El nombre del estudio contiene caracteres no válidos")
            .When(x => !string.IsNullOrEmpty(x.Studio));

        // Validación de la puntuación (opcional)
        RuleFor(x => x.Score)
            .InclusiveBetween(0, 100)
            .WithMessage("La puntuación debe estar entre 0 y 100")
            .When(x => x.Score.HasValue);

        // Validación del año (opcional)
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.Now.Year + 5)
            .WithMessage($"El año debe estar entre 1900 y {DateTime.Now.Year + 5}")
            .When(x => x.Year.HasValue);

        // Validación de que al menos un campo esté presente
        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("Debe proporcionar al menos un campo para actualizar")
            .WithName("UpdateFields");

        // Validaciones de negocio adicionales
        RuleFor(x => x)
            .Must(HaveReasonableScoreForYear)
            .WithMessage("Las películas muy antiguas (antes de 1950) raramente tienen puntuaciones muy altas")
            .WithName("Score/Year")
            .When(x => x.Score.HasValue && x.Year.HasValue);
    }

    /// <summary>
    /// Valida que al menos un campo esté presente para la actualización
    /// </summary>
    private static bool HaveAtLeastOneField(UpdateMovieDto dto)
    {
        return !string.IsNullOrEmpty(dto.Film) ||
               !string.IsNullOrEmpty(dto.Genre) ||
               !string.IsNullOrEmpty(dto.Studio) ||
               dto.Score.HasValue ||
               dto.Year.HasValue;
    }

    /// <summary>
    /// Valida que el nombre de la película sea válido
    /// </summary>
    private static bool BeValidFilmName(string? filmName)
    {
        if (string.IsNullOrWhiteSpace(filmName))
            return false;

        // No debe contener solo números
        if (filmName.All(char.IsDigit))
            return false;

        // No debe contener caracteres especiales peligrosos
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n' };
        return !filmName.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Valida que el género sea válido
    /// </summary>
    private static bool BeValidGenre(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return false;

        // Lista de géneros válidos (se puede expandir)
        var validGenres = new[]
        {
            "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Documentary",
            "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical",
            "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western",
            // Géneros en español
            "Acción", "Aventura", "Animación", "Biografía", "Comedia", "Crimen", "Documental",
            "Drama", "Familiar", "Fantasía", "Historia", "Terror", "Música", "Misterio",
            "Romance", "Ciencia Ficción", "Deporte", "Suspenso", "Guerra", "Western"
        };

        // Permitir géneros múltiples separados por coma, guión o barra
        var genres = genre.Split(new[] { ',', '|', '/', '-' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(g => g.Trim())
                          .ToArray();

        // Al menos uno de los géneros debe ser válido
        return genres.Any(g => validGenres.Contains(g, StringComparer.OrdinalIgnoreCase) || 
                              g.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)));
    }

    /// <summary>
    /// Valida que el nombre del estudio sea válido
    /// </summary>
    private static bool BeValidStudioName(string? studioName)
    {
        if (string.IsNullOrWhiteSpace(studioName))
            return false;

        // No debe contener solo números
        if (studioName.All(char.IsDigit))
            return false;

        // Debe contener al menos una letra
        if (!studioName.Any(char.IsLetter))
            return false;

        // No debe contener caracteres especiales peligrosos
        var invalidChars = new[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n' };
        return !studioName.Any(c => invalidChars.Contains(c));
    }

    /// <summary>
    /// Validación de negocio: películas muy antiguas raramente tienen puntuaciones muy altas
    /// </summary>
    private static bool HaveReasonableScoreForYear(UpdateMovieDto movie)
    {
        if (!movie.Score.HasValue || !movie.Year.HasValue)
            return true;

        // Si la película es anterior a 1950 y tiene una puntuación muy alta, es sospechoso
        if (movie.Year < 1950 && movie.Score > 95)
            return false;

        // Si la película es muy reciente (próximos años) y tiene puntuación perfecta, es sospechoso
        if (movie.Year > DateTime.Now.Year + 2 && movie.Score == 100)
            return false;

        return true;
    }
} 