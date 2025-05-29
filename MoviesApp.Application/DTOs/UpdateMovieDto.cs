using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO para actualizar una película existente
/// </summary>
public class UpdateMovieDto
{
    /// <summary>
    /// Nombre de la película
    /// </summary>
    [StringLength(255, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 255 caracteres")]
    public string? Film { get; set; }

    /// <summary>
    /// Género de la película
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "El género debe tener entre 1 y 100 caracteres")]
    public string? Genre { get; set; }

    /// <summary>
    /// Estudio que produjo la película
    /// </summary>
    [StringLength(150, MinimumLength = 1, ErrorMessage = "El estudio debe tener entre 1 y 150 caracteres")]
    public string? Studio { get; set; }

    /// <summary>
    /// Puntuación de la audiencia (0 - 100)
    /// </summary>
    [Range(0, 100, ErrorMessage = "La puntuación debe estar entre 0 y 100")]
    public int? Score { get; set; }

    /// <summary>
    /// Año de estreno de la película
    /// </summary>
    [Range(1900, 2100, ErrorMessage = "El año debe estar entre 1900 y 2100")]
    public int? Year { get; set; }
} 