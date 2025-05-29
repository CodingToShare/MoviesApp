namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO que representa el resultado de una operación de carga desde CSV
/// </summary>
public class CsvLoadResultDto
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Número total de registros procesados
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Número de registros insertados exitosamente
    /// </summary>
    public int SuccessfulInserts { get; set; }

    /// <summary>
    /// Número de registros que fallaron
    /// </summary>
    public int FailedInserts { get; set; }

    /// <summary>
    /// Número de registros duplicados encontrados
    /// </summary>
    public int DuplicatesFound { get; set; }

    /// <summary>
    /// Lista de errores encontrados durante el procesamiento
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Lista de advertencias encontradas durante el procesamiento
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Películas que se insertaron exitosamente
    /// </summary>
    public List<MovieDto> InsertedMovies { get; set; } = new();

    /// <summary>
    /// Tiempo que tomó la operación
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Fecha y hora de la operación
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
} 