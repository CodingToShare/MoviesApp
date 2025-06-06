using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApp.Application.Helpers;
using MoviesApp.Domain.Entities;
using MoviesApp.Functions.Models;
using MoviesApp.Infrastructure.Data;

namespace MoviesApp.Functions.Helpers;

/// <summary>
/// Servicio para procesamiento de archivos CSV
/// </summary>
public class CsvProcessingService
{
    private readonly MoviesDbContext _context;
    private readonly ILogger<CsvProcessingService> _logger;

    public CsvProcessingService(MoviesDbContext context, ILogger<CsvProcessingService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Procesa un archivo CSV y actualiza la base de datos
    /// </summary>
    public async Task<CsvProcessingResult> ProcessCsvAsync(Stream csvStream, string fileName)
    {
        var result = new CsvProcessingResult { FileName = fileName };

        try
        {
            // Leer registros del CSV
            var records = ReadCsvRecords(csvStream);
            result.TotalRecords = records.Count;

            _logger.LogInformation("Procesando {TotalRecords} registros del archivo {FileName}", 
                result.TotalRecords, fileName);

            // Procesar cada registro
            foreach (var record in records)
            {
                try
                {
                    await ProcessSingleRecordAsync(record, result);
                }
                catch (InvalidOperationException ex)
                {
                    result.Errors.Add($"Error de operación procesando registro {record.ToDescription()}: {ex.Message}");
                    result.ErrorCount++;
                    _logger.LogError(ex, "Error de operación procesando registro {RecordDescription}", record.ToDescription());
                }
                catch (ArgumentException ex)
                {
                    result.Errors.Add($"Error de argumento procesando registro {record.ToDescription()}: {ex.Message}");
                    result.ErrorCount++;
                    _logger.LogError(ex, "Error de argumento procesando registro {RecordDescription}", record.ToDescription());
                }
                catch (FormatException ex)
                {
                    result.Errors.Add($"Error de formato procesando registro {record.ToDescription()}: {ex.Message}");
                    result.ErrorCount++;
                    _logger.LogError(ex, "Error de formato procesando registro {RecordDescription}", record.ToDescription());
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Procesamiento completado. Creados: {Created}, Actualizados: {Updated}, Errores: {Errors}",
                result.CreatedCount, result.UpdatedCount, result.ErrorCount);

        }
        catch (InvalidOperationException ex)
        {
            result.Errors.Add($"Error de operación procesando archivo: {ex.Message}");
            _logger.LogError(ex, "Error de operación procesando archivo {FileName}", fileName);
        }
        catch (ArgumentException ex)
        {
            result.Errors.Add($"Error de argumento procesando archivo: {ex.Message}");
            _logger.LogError(ex, "Error de argumento procesando archivo {FileName}", fileName);
        }
        catch (IOException ex)
        {
            result.Errors.Add($"Error de E/S procesando archivo: {ex.Message}");
            _logger.LogError(ex, "Error de E/S procesando archivo {FileName}", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            result.Errors.Add($"Error de permisos procesando archivo: {ex.Message}");
            _logger.LogError(ex, "Error de permisos procesando archivo {FileName}", fileName);
        }

        return result;
    }

    /// <summary>
    /// Procesa un solo registro del CSV
    /// </summary>
    private async Task ProcessSingleRecordAsync(CsvMovieRecord record, CsvProcessingResult result)
    {
        // Normalizar datos del registro
        record.Normalize();

        // Validar registro después de normalización
        if (!record.IsValid())
        {
            var validationErrors = record.GetValidationErrors();
            var errorMessage = $"Registro inválido: {record.ToDescription()} - Errores: {string.Join(", ", validationErrors)}";
            result.Errors.Add(errorMessage);
            result.ErrorCount++;
            _logger.LogWarning("❌ {ErrorMessage}", errorMessage);
            return;
        }

        try
        {
            // Verificar si la película ya existe
            var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == record.Id);

            if (existingMovie != null)
            {
                // Actualizar película existente solo si hay cambios
                bool hasChanges = existingMovie.Film != record.Film ||
                                  existingMovie.Genre != record.Genre ||
                                  existingMovie.Studio != record.Studio ||
                                  existingMovie.Score != record.Score ||
                                  existingMovie.Year != record.Year;

                if (hasChanges)
                {
                    existingMovie.Film = record.Film;
                    existingMovie.Genre = record.Genre;
                    existingMovie.Studio = record.Studio;
                    existingMovie.Score = record.Score;
                    existingMovie.Year = record.Year;
                    existingMovie.MarkAsUpdated();

                    result.UpdatedCount++;
                    _logger.LogDebug("🔄 Película actualizada: {MovieDescription}", record.ToDescription());
                }
                else
                {
                    // Registro ya existe y es idéntico, no hacer nada
                    _logger.LogDebug("⏭️ Película sin cambios: {MovieDescription}", record.ToDescription());
                }
            }
            else
            {
                // Crear nueva película
                var newMovie = new Movie
                {
                    Id = record.Id,
                    Film = record.Film,
                    Genre = record.Genre,
                    Studio = record.Studio,
                    Score = record.Score,
                    Year = record.Year
                };

                _context.Movies.Add(newMovie);
                result.CreatedCount++;
                _logger.LogDebug("✅ Película creada: {MovieDescription}", record.ToDescription());
            }
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Error de operación procesando registro {record.ToDescription()}: {ex.Message}";
            result.Errors.Add(errorMessage);
            result.ErrorCount++;
            _logger.LogError(ex, "💥 {ErrorMessage}", errorMessage);
        }
        catch (ArgumentException ex)
        {
            var errorMessage = $"Error de argumento procesando registro {record.ToDescription()}: {ex.Message}";
            result.Errors.Add(errorMessage);
            result.ErrorCount++;
            _logger.LogError(ex, "💥 {ErrorMessage}", errorMessage);
        }
        catch (FormatException ex)
        {
            var errorMessage = $"Error de formato procesando registro {record.ToDescription()}: {ex.Message}";
            result.Errors.Add(errorMessage);
            result.ErrorCount++;
            _logger.LogError(ex, "💥 {ErrorMessage}", errorMessage);
        }
    }

    /// <summary>
    /// Lee los registros del CSV usando CsvHelper
    /// </summary>
    private List<CsvMovieRecord> ReadCsvRecords(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null, // Ignorar campos faltantes
            BadDataFound = context => // Manejar datos malformados
            {
                _logger.LogWarning("⚠️ Datos malformados en línea {Row}: {RawRecord}", 
                    context.Context.Parser.Row, context.RawRecord);
            },
            TrimOptions = TrimOptions.Trim,
            HeaderValidated = null, // No validar headers automáticamente
            IgnoreBlankLines = true, // Ignorar líneas en blanco
            DetectColumnCountChanges = false // No detectar cambios en el número de columnas
        });

        try
        {
            var records = csv.GetRecords<CsvMovieRecord>().ToList();
            _logger.LogInformation("📖 Se leyeron {Count} registros del CSV", records.Count);
            return records;
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError(ex, "💥 Error de CsvHelper leyendo archivo CSV");
            throw new InvalidOperationException($"Error de formato CSV: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "💥 Error de E/S leyendo archivo CSV");
            throw new InvalidOperationException($"Error de E/S leyendo archivo CSV: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "💥 Error de permisos leyendo archivo CSV");
            throw new InvalidOperationException($"Error de permisos leyendo archivo CSV: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Realiza limpieza de datos en la base de datos
    /// </summary>
    public async Task<CleanupResult> PerformDataCleanupAsync()
    {
        var result = new CleanupResult();

        try
        {
            _logger.LogInformation("Iniciando limpieza de datos");

            // Encontrar duplicados por nombre de película
            var duplicates = await _context.Movies
                .GroupBy(m => new { m.Film, m.Year })
                .Where(g => g.Count() > 1)
                .ToListAsync();

            // Usar Select para transformar explícitamente cada grupo en una lista ordenada de películas
            foreach (var movies in duplicates.Select(group => group.OrderBy(m => m.Id).ToList()))
            {
                var duplicatesToRemove = movies.Skip(1).ToList();

                // Marcar la primera película (la que se mantiene) como actualizada para reflejar el proceso de limpieza
                var movieToKeep = movies.First();
                movieToKeep.MarkAsUpdated();
                _logger.LogInformation("Película mantenida durante limpieza de duplicados: {Film} (ID: {Id})", 
                    SecurityHelper.SanitizeForLogging(movieToKeep.Film), movieToKeep.Id);

                foreach (var duplicate in duplicatesToRemove)
                {
                    _context.Movies.Remove(duplicate);
                    result.DuplicatesRemoved++;
                    _logger.LogInformation("Duplicado removido: {Film} (ID: {Id})", 
                        SecurityHelper.SanitizeForLogging(duplicate.Film), duplicate.Id);
                }
            }

            // Corregir puntajes inválidos
            var invalidScores = await _context.Movies
                .Where(m => m.Score < 0 || m.Score > 100)
                .ToListAsync();

            foreach (var movie in invalidScores)
            {
                var oldScore = movie.Score;
                movie.Score = Math.Max(0, Math.Min(100, movie.Score));
                movie.MarkAsUpdated();
                result.ScoresCorrected++;
                _logger.LogInformation("Puntaje corregido: {Film} - {OldScore} → {NewScore}", 
                    SecurityHelper.SanitizeForLogging(movie.Film), oldScore, movie.Score);
            }

            // Corregir años inválidos
            var currentYear = DateTime.Now.Year;
            var invalidYears = await _context.Movies
                .Where(m => m.Year < 1888 || m.Year > currentYear + 10)
                .ToListAsync();

            foreach (var movie in invalidYears)
            {
                var oldYear = movie.Year;
                movie.Year = Math.Max(1888, Math.Min(currentYear, movie.Year));
                movie.MarkAsUpdated();
                result.YearsCorrected++;
                _logger.LogInformation("Año corregido: {Film} - {OldYear} → {NewYear}", 
                    SecurityHelper.SanitizeForLogging(movie.Film), oldYear, movie.Year);
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            _logger.LogInformation("Limpieza completada: {DuplicatesRemoved} duplicados, {ScoresCorrected} puntajes, {YearsCorrected} años corregidos",
                result.DuplicatesRemoved, result.ScoresCorrected, result.YearsCorrected);

        }
        catch (InvalidOperationException ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error de operación durante la limpieza de datos");
        }
        catch (ArgumentException ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error de argumento durante la limpieza de datos");
        }
        catch (TimeoutException ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Timeout durante la limpieza de datos");
        }

        return result;
    }
}

/// <summary>
/// Resultado del procesamiento de CSV
/// </summary>
public class CsvProcessingResult
{
    public string FileName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => ErrorCount == 0 || (CreatedCount + UpdatedCount) > 0;
}

/// <summary>
/// Resultado de la limpieza de datos
/// </summary>
public class CleanupResult
{
    public bool Success { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int ScoresCorrected { get; set; }
    public int YearsCorrected { get; set; }
    public string? ErrorMessage { get; set; }
} 