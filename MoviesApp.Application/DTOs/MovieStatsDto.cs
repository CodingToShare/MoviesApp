namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO que representa estadísticas de las películas
/// </summary>
public class MovieStatsDto
{
    /// <summary>
    /// Número total de películas
    /// </summary>
    public int TotalMovies { get; set; }

    /// <summary>
    /// Puntuación promedio de todas las películas
    /// </summary>
    public double AverageScore { get; set; }

    /// <summary>
    /// Puntuación más alta
    /// </summary>
    public int HighestScore { get; set; }

    /// <summary>
    /// Puntuación más baja
    /// </summary>
    public int LowestScore { get; set; }

    /// <summary>
    /// Año más antiguo
    /// </summary>
    public int OldestYear { get; set; }

    /// <summary>
    /// Año más reciente
    /// </summary>
    public int NewestYear { get; set; }

    /// <summary>
    /// Número de géneros únicos
    /// </summary>
    public int UniqueGenres { get; set; }

    /// <summary>
    /// Número de estudios únicos
    /// </summary>
    public int UniqueStudios { get; set; }

    /// <summary>
    /// Géneros más populares (top 5)
    /// </summary>
    public List<GenreStatsDto> TopGenres { get; set; } = new();

    /// <summary>
    /// Estudios más productivos (top 5)
    /// </summary>
    public List<StudioStatsDto> TopStudios { get; set; } = new();

    /// <summary>
    /// Distribución de películas por década
    /// </summary>
    public List<DecadeStatsDto> MoviesByDecade { get; set; } = new();

    /// <summary>
    /// Fecha de generación de las estadísticas
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO para estadísticas por género
/// </summary>
public class GenreStatsDto
{
    /// <summary>
    /// Nombre del género
    /// </summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Número de películas del género
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Puntuación promedio del género
    /// </summary>
    public double AverageScore { get; set; }
}

/// <summary>
/// DTO para estadísticas por estudio
/// </summary>
public class StudioStatsDto
{
    /// <summary>
    /// Nombre del estudio
    /// </summary>
    public string Studio { get; set; } = string.Empty;

    /// <summary>
    /// Número de películas del estudio
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Puntuación promedio del estudio
    /// </summary>
    public double AverageScore { get; set; }
}

/// <summary>
/// DTO para estadísticas por década
/// </summary>
public class DecadeStatsDto
{
    /// <summary>
    /// Década (ej: 1990, 2000, 2010)
    /// </summary>
    public int Decade { get; set; }

    /// <summary>
    /// Número de películas en la década
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Puntuación promedio de la década
    /// </summary>
    public double AverageScore { get; set; }
} 