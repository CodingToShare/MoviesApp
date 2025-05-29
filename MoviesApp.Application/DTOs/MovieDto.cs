namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO para representar una película en las respuestas de la API
/// </summary>
public class MovieDto
{
    /// <summary>
    /// Identificador único de la tabla
    /// </summary>
    public Guid MovieId { get; set; }

    /// <summary>
    /// Identificador único de la película (Negocio)
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre de la película
    /// </summary>
    public string Film { get; set; } = string.Empty;

    /// <summary>
    /// Género de la película
    /// </summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Estudio que produjo la película
    /// </summary>
    public string Studio { get; set; } = string.Empty;

    /// <summary>
    /// Puntuación de la audiencia (0 - 100)
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Año de estreno de la película
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización del registro
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
} 