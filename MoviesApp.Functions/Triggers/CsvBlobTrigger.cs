using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MoviesApp.Functions.Helpers;
using MoviesApp.Infrastructure.Data;

namespace MoviesApp.Functions.Triggers;

/// <summary>
/// Función que se activa cuando se sube un archivo CSV al Blob Storage
/// </summary>
public class CsvBlobTrigger
{
    private readonly MoviesDbContext _context;
    private readonly CsvProcessingService _csvProcessingService;
    private readonly ILogger<CsvBlobTrigger> _logger;

    public CsvBlobTrigger(
        MoviesDbContext context,
        CsvProcessingService csvProcessingService,
        ILogger<CsvBlobTrigger> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _csvProcessingService = csvProcessingService ?? throw new ArgumentNullException(nameof(csvProcessingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Se activa cuando se sube un archivo CSV al contenedor movies-csv
    /// </summary>
    /// <param name="myBlob">Stream del archivo CSV</param>
    /// <param name="name">Nombre del archivo</param>
    /// <param name="uri">URI del blob</param>
    [Function("CsvBlobTrigger")]
    public async Task Run(
        [BlobTrigger("movies-csv/{name}", Connection = "BlobStorageConnectionString")] Stream myBlob,
        string name,
        Uri uri)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🎬 INICIO - Procesando archivo CSV: {FileName} desde {Uri}", name, uri);

        try
        {
            // Validar que es un archivo CSV
            if (!IsValidCsvFile(name))
            {
                _logger.LogWarning("❌ Archivo ignorado - No es un CSV válido: {FileName}", name);
                return;
            }

            // Leer todo el contenido a memoria para poder validar el tamaño
            // Esto es necesario porque Stream.Length no está soportado en Azure Functions blob triggers
            var memoryStream = new MemoryStream();
            await myBlob.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var fileSize = memoryStream.Length;
            _logger.LogInformation("📏 Tamaño del archivo: {FileName} ({Size} bytes)", name, fileSize);

            // Verificar tamaño del archivo (máximo 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (fileSize > maxFileSize)
            {
                _logger.LogError("❌ Archivo demasiado grande: {FileName} ({Size} bytes). Máximo permitido: {MaxSize} bytes",
                    name, fileSize, maxFileSize);
                return;
            }

            // Procesar el archivo CSV usando el MemoryStream
            var result = await _csvProcessingService.ProcessCsvAsync(memoryStream, name);

            // Log del resultado con análisis detallado
            LogProcessingResult(result, startTime, fileSize);

            // Análisis adicional para el CSV específico de movies.csv
            if (name.Contains("movies.csv", StringComparison.OrdinalIgnoreCase))
            {
                LogMoviesCsvSpecificAnalysis(result);
            }

            // Opcional: Enviar notificación o guardar log en tabla separada
            await SaveProcessingLogAsync(result, startTime, uri.ToString());

        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "💥 ERROR - Fallo procesando archivo CSV: {FileName} (Duración: {Duration}ms)",
                name, duration.TotalMilliseconds);

            // Opcional: Enviar alerta o guardar error en tabla de logs
            await SaveErrorLogAsync(name, ex.Message, uri.ToString());
        }
    }

    /// <summary>
    /// Valida si el archivo es un CSV válido
    /// </summary>
    private static bool IsValidCsvFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".csv";
    }

    /// <summary>
    /// Registra el resultado del procesamiento
    /// </summary>
    private void LogProcessingResult(CsvProcessingResult result, DateTime startTime, long fileSize)
    {
        var duration = DateTime.UtcNow - startTime;
        var durationMs = (int)duration.TotalMilliseconds;

        if (result.Success)
        {
            _logger.LogInformation(
                "✅ ÉXITO - Archivo procesado: {FileName} ({Size} bytes)\n" +
                "📊 Estadísticas:\n" +
                "   • Total registros: {TotalRecords}\n" +
                "   • Creados: {CreatedCount}\n" +
                "   • Actualizados: {UpdatedCount}\n" +
                "   • Errores: {ErrorCount}\n" +
                "   • Duración: {Duration}ms",
                result.FileName, fileSize, result.TotalRecords, result.CreatedCount, 
                result.UpdatedCount, result.ErrorCount, durationMs);
        }
        else
        {
            _logger.LogError(
                "❌ ERRORES - Archivo procesado con errores: {FileName} ({Size} bytes)\n" +
                "📊 Estadísticas:\n" +
                "   • Total registros: {TotalRecords}\n" +
                "   • Creados: {CreatedCount}\n" +
                "   • Actualizados: {UpdatedCount}\n" +
                "   • Errores: {ErrorCount}\n" +
                "   • Duración: {Duration}ms\n" +
                "🔍 Errores encontrados:\n{Errors}",
                result.FileName, fileSize, result.TotalRecords, result.CreatedCount,
                result.UpdatedCount, result.ErrorCount, durationMs,
                string.Join("\n   • ", result.Errors));
        }
    }

    /// <summary>
    /// Guarda un log del procesamiento en la base de datos (opcional)
    /// </summary>
    private Task SaveProcessingLogAsync(CsvProcessingResult result, DateTime startTime, string blobUri)
    {
        try
        {
            // Aquí podrías crear una tabla de logs para tracking
            // Por ahora solo loggeamos que se podría implementar
            _logger.LogDebug("💾 Log de procesamiento guardado para {FileName}", result.FileName);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo guardar el log de procesamiento para {FileName}", result.FileName);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Guarda un log de error en la base de datos (opcional)
    /// </summary>
    private Task SaveErrorLogAsync(string fileName, string errorMessage, string blobUri)
    {
        try
        {
            // Aquí podrías crear una tabla de logs de errores
            _logger.LogDebug("💾 Log de error guardado para {FileName}", fileName);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No se pudo guardar el log de error para {FileName}", fileName);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Análisis específico para el archivo movies.csv
    /// </summary>
    private void LogMoviesCsvSpecificAnalysis(CsvProcessingResult result)
    {
        _logger.LogInformation(
            "🎬 Análisis específico del archivo movies.csv:\n" +
            "   • Se esperaban ~77 registros de películas\n" +
            "   • Registros procesados: {TotalRecords}\n" +
            "   • Posibles duplicados detectados: {DuplicateCount}\n" +
            "   • Errores de validación: {ErrorCount}\n" +
            "   • Tasa de éxito: {SuccessRate:P1}",
            result.TotalRecords,
            EstimateDuplicatesFromErrors(result.Errors),
            result.ErrorCount,
            (double)(result.CreatedCount + result.UpdatedCount) / Math.Max(1, result.TotalRecords));

        // Log específico de errores comunes encontrados
        if (result.Errors.Any())
        {
            var commonErrors = AnalyzeCommonErrors(result.Errors);
            _logger.LogInformation("🔍 Errores comunes detectados:\n{CommonErrors}", commonErrors);
        }
    }

    /// <summary>
    /// Estima duplicados basado en los errores
    /// </summary>
    private int EstimateDuplicatesFromErrors(List<string> errors)
    {
        return errors.Count(e => e.Contains("duplicado", StringComparison.OrdinalIgnoreCase) ||
                                e.Contains("ya existe", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Analiza errores comunes en el CSV
    /// </summary>
    private string AnalyzeCommonErrors(List<string> errors)
    {
        var analysis = new List<string>();

        var genreErrors = errors.Count(e => e.Contains("Romence") || e.Contains("Comdy") || e.Contains("romance") || e.Contains("comedy"));
        if (genreErrors > 0)
        {
            analysis.Add($"   • Errores tipográficos en géneros: {genreErrors}");
        }

        var duplicateErrors = errors.Count(e => e.Contains("Mamma Mia") || e.Contains("Gnomeo"));
        if (duplicateErrors > 0)
        {
            analysis.Add($"   • Películas duplicadas: {duplicateErrors}");
        }

        var validationErrors = errors.Count(e => e.Contains("inválido"));
        if (validationErrors > 0)
        {
            analysis.Add($"   • Errores de validación: {validationErrors}");
        }

        return analysis.Any() ? string.Join("\n", analysis) : "   • No se detectaron patrones de errores comunes";
    }
} 