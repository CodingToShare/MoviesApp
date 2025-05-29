using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Domain.Entities;

/// <summary>
/// Entidad que representa una película en el dominio
/// </summary>
public class Movie
{
    /// <summary>
    /// Identificador único de la tabla (Primary Key)
    /// </summary>
    [Key]
    public Guid MovieId { get; set; }

    /// <summary>
    /// Identificador único de la película (Negocio)
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Nombre de la película
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Film { get; set; } = string.Empty;

    /// <summary>
    /// Género de la película
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Estudio que produjo la película
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Studio { get; set; } = string.Empty;

    /// <summary>
    /// Puntuación de la audiencia (0 - 100)
    /// </summary>
    [Required]
    [Range(0, 100)]
    public int Score { get; set; }

    /// <summary>
    /// Año de estreno de la película
    /// </summary>
    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }

    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización del registro
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public Movie()
    {
        MovieId = Guid.NewGuid();
    }

    /// <summary>
    /// Constructor con parámetros
    /// </summary>
    public Movie(int id, string film, string genre, string studio, int score, int year)
        : this()
    {
        Id = id;
        Film = film;
        Genre = genre;
        Studio = studio;
        Score = score;
        Year = year;
    }

    /// <summary>
    /// Método para actualizar la fecha de modificación
    /// </summary>
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validar que la película tenga datos válidos
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Film) &&
               !string.IsNullOrWhiteSpace(Genre) &&
               !string.IsNullOrWhiteSpace(Studio) &&
               Score >= 0 && Score <= 100 &&
               Year >= 1900 && Year <= 2100;
    }

    /// <summary>
    /// Override ToString para debugging
    /// </summary>
    public override string ToString()
    {
        return $"Movie: {Film} ({Year}) - {Genre} - Score: {Score}";
    }
} 