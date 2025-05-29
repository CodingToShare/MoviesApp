namespace MoviesApp.Application.Validators;

/// <summary>
/// Clase base con métodos de validación comunes para películas
/// </summary>
public static class BaseMovieValidator
{
    /// <summary>
    /// Lista de géneros válidos
    /// </summary>
    public static readonly string[] ValidGenres = new[]
    {
        // Géneros en inglés
        "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Documentary",
        "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical",
        "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western",
        // Géneros en español
        "Acción", "Aventura", "Animación", "Biografía", "Comedia", "Crimen", "Documental",
        "Drama", "Familiar", "Fantasía", "Historia", "Terror", "Música", "Misterio",
        "Romance", "Ciencia Ficción", "Deporte", "Suspenso", "Guerra", "Western"
    };

    /// <summary>
    /// Caracteres no válidos para nombres y textos
    /// </summary>
    public static readonly string[] InvalidChars = new[] 
    { 
        "<", ">", "\"", "'", "&", "\0", "\r", "\n" 
    };

    /// <summary>
    /// Campos válidos para ordenamiento
    /// </summary>
    public static readonly string[] ValidOrderByFields = new[] 
    { 
        "id", "film", "genre", "studio", "score", "year", "createdat", "updatedat",
        "Id", "Film", "Genre", "Studio", "Score", "Year", "CreatedAt", "UpdatedAt"
    };

    /// <summary>
    /// Órdenes válidos
    /// </summary>
    public static readonly string[] ValidOrders = new[] 
    { 
        "asc", "desc", "ascending", "descending" 
    };

    /// <summary>
    /// Valida que el nombre de la película sea válido
    /// </summary>
    public static bool IsValidFilmName(string? filmName)
    {
        if (string.IsNullOrWhiteSpace(filmName))
            return false;

        // No debe contener solo números
        if (filmName.All(char.IsDigit))
            return false;

        // No debe contener caracteres especiales peligrosos
        return !filmName.Any(c => InvalidChars.Contains(c.ToString()));
    }

    /// <summary>
    /// Valida que el género sea válido
    /// </summary>
    public static bool IsValidGenre(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return false;

        // Permitir géneros múltiples separados por coma, guión o barra
        var genres = genre.Split(new[] { ',', '|', '/', '-' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(g => g.Trim())
                          .ToArray();

        // Al menos uno de los géneros debe ser válido
        return genres.Any(g => ValidGenres.Contains(g, StringComparer.OrdinalIgnoreCase) || 
                              g.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)));
    }

    /// <summary>
    /// Valida que el nombre del estudio sea válido
    /// </summary>
    public static bool IsValidStudioName(string? studioName)
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
        return !studioName.Any(c => InvalidChars.Contains(c.ToString()));
    }

    /// <summary>
    /// Valida que la combinación de año y puntuación sea razonable
    /// </summary>
    public static bool IsReasonableScoreForYear(int? year, int? score)
    {
        if (!year.HasValue || !score.HasValue)
            return true;

        // Si la película es anterior a 1950 y tiene una puntuación muy alta, es sospechoso
        if (year < 1950 && score > 95)
            return false;

        // Si la película es muy reciente (próximos años) y tiene puntuación perfecta, es sospechoso
        if (year > DateTime.Now.Year + 2 && score == 100)
            return false;

        return true;
    }

    /// <summary>
    /// Valida que el orden sea válido
    /// </summary>
    public static bool IsValidOrder(string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
            return false;

        return ValidOrders.Contains(order.ToLowerInvariant());
    }

    /// <summary>
    /// Valida que el campo de ordenamiento sea válido
    /// </summary>
    public static bool IsValidOrderByField(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return false;

        return ValidOrderByFields.Contains(orderBy);
    }

    /// <summary>
    /// Valida que el filtro de género sea válido (para consultas)
    /// </summary>
    public static bool IsValidGenreFilter(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            return false;

        // No debe contener caracteres especiales peligrosos para SQL injection
        var dangerousChars = new[] { "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", "--", "/*", "*/" };
        return !genre.Any(c => dangerousChars.Contains(c.ToString()));
    }

    /// <summary>
    /// Obtiene el año actual más un margen para películas futuras
    /// </summary>
    public static int GetMaxValidYear() => DateTime.Now.Year + 5;

    /// <summary>
    /// Obtiene el año mínimo válido para películas
    /// </summary>
    public static int GetMinValidYear() => 1900;
} 