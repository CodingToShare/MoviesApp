using FluentValidation;

namespace MoviesApp.Application.Validators;

/// <summary>
/// DTO para parámetros de consulta de películas
/// </summary>
public class MovieQueryDto
{
    public int? Id { get; set; }
    public int? Total { get; set; }
    public string? Order { get; set; }
    public string? OrderBy { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public int? MinScore { get; set; }
}

/// <summary>
/// Validador para parámetros de consulta de películas
/// </summary>
public class MovieQueryValidator : AbstractValidator<MovieQueryDto>
{
    public MovieQueryValidator()
    {
        // Validación del ID
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("El ID debe ser mayor a 0")
            .When(x => x.Id.HasValue);

        // Validación del total
        RuleFor(x => x.Total)
            .InclusiveBetween(1, 1000)
            .WithMessage("El total debe estar entre 1 y 1000")
            .When(x => x.Total.HasValue);

        // Validación del orden
        RuleFor(x => x.Order)
            .Must(BeValidOrder)
            .WithMessage("El orden debe ser 'asc' o 'desc'")
            .When(x => !string.IsNullOrEmpty(x.Order));

        // Validación del campo de ordenamiento
        RuleFor(x => x.OrderBy)
            .Must(BeValidOrderByField)
            .WithMessage("El campo de ordenamiento no es válido. Campos válidos: Id, Film, Genre, Studio, Score, Year, CreatedAt")
            .When(x => !string.IsNullOrEmpty(x.OrderBy));

        // Validación del género
        RuleFor(x => x.Genre)
            .Length(1, 100)
            .WithMessage("El género debe tener entre 1 y 100 caracteres")
            .Must(BeValidGenreFilter)
            .WithMessage("El género contiene caracteres no válidos")
            .When(x => !string.IsNullOrEmpty(x.Genre));

        // Validación del año
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.Now.Year + 5)
            .WithMessage($"El año debe estar entre 1900 y {DateTime.Now.Year + 5}")
            .When(x => x.Year.HasValue);

        // Validación de la puntuación mínima
        RuleFor(x => x.MinScore)
            .InclusiveBetween(0, 100)
            .WithMessage("La puntuación mínima debe estar entre 0 y 100")
            .When(x => x.MinScore.HasValue);
    }

    /// <summary>
    /// Valida que el orden sea válido
    /// </summary>
    private static bool BeValidOrder(string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return false;

        var validOrders = new[] { "asc", "desc", "ascending", "descending" };
        return validOrders.Contains(order.ToLowerInvariant());
    }

    /// <summary>
    /// Valida que el campo de ordenamiento sea válido
    /// </summary>
    private static bool BeValidOrderByField(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return false;

        var validFields = new[] 
        { 
            "id", "film", "genre", "studio", "score", "year", "createdat", "updatedat",
            "Id", "Film", "Genre", "Studio", "Score", "Year", "CreatedAt", "UpdatedAt"
        };
        
        return validFields.Contains(orderBy);
    }

    /// <summary>
    /// Valida que el filtro de género sea válido
    /// </summary>
    private static bool BeValidGenreFilter(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return false;

        // No debe contener caracteres especiales peligrosos
        var invalidChars = new[] { "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", "--" };
        return !genre.Any(c => invalidChars.Contains(c.ToString()));
    }
}

/// <summary>
/// Validador específico para búsqueda por ID
/// </summary>
public class MovieByIdQueryValidator : AbstractValidator<int>
{
    public MovieByIdQueryValidator()
    {
        RuleFor(id => id)
            .GreaterThan(0)
            .WithMessage("El ID debe ser mayor a 0")
            .LessThanOrEqualTo(999999)
            .WithMessage("El ID no puede ser mayor a 999999");
    }
} 