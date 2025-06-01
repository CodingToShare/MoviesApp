using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Logging;
using MoviesApp.Domain.Entities;
using MoviesApp.Infrastructure.Repositories;
using System.Globalization;

namespace MoviesApp.Infrastructure.Helpers;

/// <summary>
/// Helper para operaciones con archivos CSV
/// </summary>
public class CsvMovieHelper
{
    private readonly ILogger<CsvMovieHelper> _logger;

    public CsvMovieHelper(ILogger<CsvMovieHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Valida el formato de un archivo CSV
    /// </summary>
    public async Task<CsvValidationResult> ValidateCsvAsync(string filePath)
    {
        var result = new CsvValidationResult();
        
        try
        {
            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.Errors.Add($"El archivo no existe: {filePath}");
                return result;
            }

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            // No necesitamos registrar ClassMap - usamos atributos en MovieCsvRecord

            // Leer encabezados
            await csv.ReadAsync();
            csv.ReadHeader();
            
            var headers = csv.HeaderRecord;
            if (headers == null || headers.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("El archivo CSV no tiene encabezados");
                return result;
            }

            // Validar encabezados requeridos
            var requiredHeaders = new[] { "ID", "Film", "Genre", "Studio", "Score", "Year" };
            var missingHeaders = requiredHeaders.Where(h => 
                !headers.Any(header => string.Equals(header, h, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (missingHeaders.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Faltan encabezados requeridos: {string.Join(", ", missingHeaders)}");
            }

            // Contar registros
            var recordCount = 0;
            while (await csv.ReadAsync())
            {
                recordCount++;
            }

            result.RecordCount = recordCount;
            result.IsValid = result.Errors.Count == 0;

            _logger.LogInformation("Validación CSV completada: {IsValid}, Registros: {Count}, Errores: {ErrorCount}", 
                result.IsValid, result.RecordCount, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar archivo CSV: {FilePath}", filePath);
            result.IsValid = false;
            result.Errors.Add($"Error al procesar el archivo: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Exporta películas a un archivo CSV
    /// </summary>
    public async Task ExportToCsvAsync(IEnumerable<Movie> movies, string filePath)
    {
        try
        {
            _logger.LogInformation("Exportando {Count} películas a CSV: {FilePath}", movies.Count(), filePath);

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // No necesitamos registrar ClassMap - usamos atributos en MovieExportRecord

            // Escribir registros
            await csv.WriteRecordsAsync(movies.Select(m => new MovieExportRecord
            {
                Id = m.Id,
                Film = m.Film,
                Genre = m.Genre,
                Studio = m.Studio,
                Score = m.Score,
                Year = m.Year,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }));

            _logger.LogInformation("Exportación CSV completada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar a CSV: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Obtiene estadísticas de un archivo CSV
    /// </summary>
    public Task<CsvStatistics> GetCsvStatisticsAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var stats = new CsvStatistics();

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                
                // No necesitamos registrar ClassMap - usamos atributos en MovieCsvRecord
                var records = csv.GetRecords<MovieCsvRecord>();

                var genreCounts = new Dictionary<string, int>();
                var studioCounts = new Dictionary<string, int>();
                var yearCounts = new Dictionary<int, int>();
                var scores = new List<int>();

                foreach (var record in records)
                {
                    stats.TotalRecords++;

                    // Estadísticas por género
                    if (!string.IsNullOrEmpty(record.Genre))
                    {
                        genreCounts[record.Genre] = genreCounts.GetValueOrDefault(record.Genre, 0) + 1;
                    }

                    // Estadísticas por estudio
                    if (!string.IsNullOrEmpty(record.Studio))
                    {
                        studioCounts[record.Studio] = studioCounts.GetValueOrDefault(record.Studio, 0) + 1;
                    }

                    // Estadísticas por año
                    yearCounts[record.Year] = yearCounts.GetValueOrDefault(record.Year, 0) + 1;

                    // Puntuaciones
                    scores.Add(record.Score);
                }

                stats.GenreDistribution = genreCounts;
                stats.StudioDistribution = studioCounts;
                stats.YearDistribution = yearCounts;
                
                if (scores.Any())
                {
                    stats.AverageScore = scores.Average();
                    stats.MinScore = scores.Min();
                    stats.MaxScore = scores.Max();
                }

                stats.YearRange = yearCounts.Keys.Any() ? 
                    new YearRange { Min = yearCounts.Keys.Min(), Max = yearCounts.Keys.Max() } : 
                    new YearRange();

                _logger.LogInformation("Estadísticas CSV calculadas: {TotalRecords} registros", stats.TotalRecords);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular estadísticas CSV: {FilePath}", filePath);
                throw;
            }
        });
    }
}

/// <summary>
/// Resultado de validación de CSV
/// </summary>
public class CsvValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int RecordCount { get; set; }
}

/// <summary>
/// Estadísticas de un archivo CSV
/// </summary>
public class CsvStatistics
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> GenreDistribution { get; set; } = new();
    public Dictionary<string, int> StudioDistribution { get; set; } = new();
    public Dictionary<int, int> YearDistribution { get; set; } = new();
    public double AverageScore { get; set; }
    public int MinScore { get; set; }
    public int MaxScore { get; set; }
    public YearRange YearRange { get; set; } = new();
}

/// <summary>
/// Rango de años
/// </summary>
public class YearRange
{
    public int Min { get; set; }
    public int Max { get; set; }
}

/// <summary>
/// Registro para exportación CSV usando atributos en lugar de ClassMap
/// para evitar virtual calls en constructor
/// </summary>
public class MovieExportRecord
{
    [Name("ID")]
    public int Id { get; set; }
    
    [Name("Film")]
    public string Film { get; set; } = string.Empty;
    
    [Name("Genre")]
    public string Genre { get; set; } = string.Empty;
    
    [Name("Studio")]
    public string Studio { get; set; } = string.Empty;
    
    [Name("Score")]
    public int Score { get; set; }
    
    [Name("Year")]
    public int Year { get; set; }
    
    [Name("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [Name("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
} 