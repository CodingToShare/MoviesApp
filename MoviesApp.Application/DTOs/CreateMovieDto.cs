using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO para crear una nueva película
/// </summary>
public class CreateMovieDto
{
    /// <summary>
    /// Identificador único de la película (Negocio)
    /// </summary>
    [Required(ErrorMessage = "El ID es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID debe ser mayor a 0")]
    public int Id { get; set; }

    /// <summary>
    /// Nombre de la película
    /// </summary>
    [Required(ErrorMessage = "El nombre de la película es requerido")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 255 caracteres")]
    public string Film { get; set; } = string.Empty;

    /// <summary>
    /// Género de la película
    /// </summary>
    [Required(ErrorMessage = "El género es requerido")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El género debe tener entre 1 y 100 caracteres")]
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Estudio que produjo la película
    /// </summary>
    [Required(ErrorMessage = "El estudio es requerido")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "El estudio debe tener entre 1 y 150 caracteres")]
    public string Studio { get; set; } = string.Empty;

    /// <summary>
    /// Puntuación de la audiencia (0 - 100)
    /// </summary>
    [Required(ErrorMessage = "La puntuación es requerida")]
    [Range(0, 100, ErrorMessage = "La puntuación debe estar entre 0 y 100")]
    public int Score { get; set; }

    /// <summary>
    /// Año de estreno de la película
    /// </summary>
    [Required(ErrorMessage = "El año es requerido")]
    [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100")]
    public int Year { get; set; }
} 