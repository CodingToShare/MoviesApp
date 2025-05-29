using System.Text;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesApp.Functions.Helpers;
using MoviesApp.Infrastructure.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace MoviesApp.Functions.Triggers;

/// <summary>
/// Función que se ejecuta diariamente para limpiar y mantener la base de datos
/// </summary>
public class CsvDailyCleanup
{
    private readonly MoviesDbContext _context;
    private readonly CsvProcessingService _csvProcessingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CsvDailyCleanup> _logger;

    public CsvDailyCleanup(
        MoviesDbContext context,
        CsvProcessingService csvProcessingService,
        IConfiguration configuration,
        ILogger<CsvDailyCleanup> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _csvProcessingService = csvProcessingService ?? throw new ArgumentNullException(nameof(csvProcessingService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Se ejecuta diariamente a las 2:00 AM para limpiar la base de datos
    /// </summary>
    [Function("CsvDailyCleanup")]
    public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🧹 INICIO - Limpieza diaria iniciada a las {StartTime}", startTime);

        try
        {
            // 1. Realizar limpieza de datos
            _logger.LogInformation("🔍 Paso 1: Ejecutando limpieza de datos...");
            var cleanupResult = await _csvProcessingService.PerformDataCleanupAsync();
            
            LogCleanupResult(cleanupResult);

            // 2. Generar estadísticas de la base de datos
            _logger.LogInformation("📊 Paso 2: Generando estadísticas...");
            var stats = await GenerateDatabaseStatsAsync();
            
            LogDatabaseStats(stats);

            // 3. Crear respaldo de datos
            _logger.LogInformation("💾 Paso 3: Creando respaldo de datos...");
            await CreateDataBackupAsync();

            // 4. Limpiar archivos antiguos del blob storage (opcional)
            _logger.LogInformation("🗑️ Paso 4: Limpiando archivos antiguos...");
            await CleanupOldBlobsAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ COMPLETADO - Limpieza diaria finalizada exitosamente (Duración: {Duration}ms)", 
                duration.TotalMilliseconds);

        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "💥 ERROR - Fallo en la limpieza diaria (Duración: {Duration}ms)", 
                duration.TotalMilliseconds);
            
            // En producción, aquí podrías enviar una alerta
            await SendCleanupErrorAlert(ex);
        }
    }

    /// <summary>
    /// Genera estadísticas de la base de datos
    /// </summary>
    private async Task<DatabaseStats> GenerateDatabaseStatsAsync()
    {
        var stats = new DatabaseStats();

        try
        {
            stats.TotalMovies = await _context.Movies.CountAsync();
            stats.MoviesThisYear = await _context.Movies.CountAsync(m => m.Year == DateTime.Now.Year);
            stats.AverageScore = await _context.Movies.AverageAsync(m => (double)m.Score);
            stats.TopGenre = await _context.Movies
                .GroupBy(m => m.Genre)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "N/A";
            
            stats.OldestMovie = await _context.Movies
                .OrderBy(m => m.Year)
                .Select(m => new { m.Film, m.Year })
                .FirstOrDefaultAsync();
            
            stats.NewestMovie = await _context.Movies
                .OrderByDescending(m => m.Year)
                .Select(m => new { m.Film, m.Year })
                .FirstOrDefaultAsync();

            stats.TopStudio = await _context.Movies
                .GroupBy(m => m.Studio)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "N/A";

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando estadísticas de la base de datos");
        }

        return stats;
    }

    /// <summary>
    /// Crea un respaldo de los datos en formato JSON
    /// </summary>
    private async Task CreateDataBackupAsync()
    {
        try
        {
            var blobConnectionString = _configuration.GetConnectionString("BlobStorage");
            if (string.IsNullOrEmpty(blobConnectionString))
            {
                _logger.LogWarning("⚠️ No se encontró connection string para Blob Storage, omitiendo respaldo");
                return;
            }

            var containerName = _configuration["BackupContainerName"] ?? "movies-backup";
            var fileName = $"movies-backup-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";

            // Obtener todas las películas
            var movies = await _context.Movies.AsNoTracking().ToListAsync();
            
            // Serializar a JSON
            var jsonData = JsonSerializer.Serialize(movies, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Subir al blob storage
            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            
            // Crear contenedor si no existe
            await containerClient.CreateIfNotExistsAsync();
            
            var blobClient = containerClient.GetBlobClient(fileName);
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
            await blobClient.UploadAsync(stream, overwrite: true);

            _logger.LogInformation("💾 Respaldo creado exitosamente: {FileName} ({Count} películas)", 
                fileName, movies.Count);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando respaldo de datos");
        }
    }

    /// <summary>
    /// Limpia archivos CSV antiguos del blob storage
    /// </summary>
    private async Task CleanupOldBlobsAsync()
    {
        try
        {
            var blobConnectionString = _configuration.GetConnectionString("BlobStorage");
            if (string.IsNullOrEmpty(blobConnectionString))
            {
                _logger.LogWarning("⚠️ No se encontró connection string para Blob Storage, omitiendo limpieza");
                return;
            }

            var csvContainerName = _configuration["CsvContainerName"] ?? "movies-csv";
            var backupContainerName = _configuration["BackupContainerName"] ?? "movies-backup";
            var retentionDays = 30; // Mantener archivos por 30 días

            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            // Limpiar CSVs antiguos
            await CleanupContainerAsync(blobServiceClient, csvContainerName, cutoffDate, "CSV");
            
            // Limpiar respaldos antiguos (mantener solo los últimos 7)
            await CleanupBackupsAsync(blobServiceClient, backupContainerName);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error limpiando archivos antiguos del blob storage");
        }
    }

    /// <summary>
    /// Limpia archivos antiguos de un contenedor
    /// </summary>
    private async Task CleanupContainerAsync(BlobServiceClient serviceClient, string containerName, DateTime cutoffDate, string fileType)
    {
        try
        {
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            
            if (!await containerClient.ExistsAsync())
                return;

            var deletedCount = 0;
            
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (blobItem.Properties.LastModified < cutoffDate)
                {
                    await containerClient.DeleteBlobIfExistsAsync(blobItem.Name);
                    deletedCount++;
                    _logger.LogDebug("🗑️ Archivo {FileType} eliminado: {BlobName}", fileType, blobItem.Name);
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("🗑️ Se eliminaron {Count} archivos {FileType} antiguos", deletedCount, fileType);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error limpiando contenedor {ContainerName}", containerName);
        }
    }

    /// <summary>
    /// Limpia respaldos antiguos, manteniendo solo los últimos 7
    /// </summary>
    private async Task CleanupBackupsAsync(BlobServiceClient serviceClient, string containerName)
    {
        try
        {
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            
            if (!await containerClient.ExistsAsync())
                return;

            var backups = new List<(string Name, DateTimeOffset? LastModified)>();
            
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                backups.Add((blobItem.Name, blobItem.Properties.LastModified));
            }

            // Mantener solo los últimos 7 respaldos
            var backupsToDelete = backups
                .OrderByDescending(b => b.LastModified)
                .Skip(7)
                .ToList();

            foreach (var backup in backupsToDelete)
            {
                await containerClient.DeleteBlobIfExistsAsync(backup.Name);
                _logger.LogDebug("🗑️ Respaldo antiguo eliminado: {BackupName}", backup.Name);
            }

            if (backupsToDelete.Any())
            {
                _logger.LogInformation("🗑️ Se eliminaron {Count} respaldos antiguos", backupsToDelete.Count);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error limpiando respaldos antiguos");
        }
    }

    /// <summary>
    /// Registra el resultado de la limpieza
    /// </summary>
    private void LogCleanupResult(CleanupResult result)
    {
        if (result.Success)
        {
            _logger.LogInformation(
                "✅ Limpieza de datos completada:\n" +
                "   • Duplicados removidos: {DuplicatesRemoved}\n" +
                "   • Puntajes corregidos: {ScoresCorrected}\n" +
                "   • Años corregidos: {YearsCorrected}",
                result.DuplicatesRemoved, result.ScoresCorrected, result.YearsCorrected);
        }
        else
        {
            _logger.LogError("❌ Error en limpieza de datos: {ErrorMessage}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// Registra las estadísticas de la base de datos
    /// </summary>
    private void LogDatabaseStats(DatabaseStats stats)
    {
        _logger.LogInformation(
            "📊 Estadísticas de la base de datos:\n" +
            "   • Total de películas: {TotalMovies}\n" +
            "   • Películas de este año: {MoviesThisYear}\n" +
            "   • Puntuación promedio: {AverageScore:F2}\n" +
            "   • Género más popular: {TopGenre}\n" +
            "   • Estudio más activo: {TopStudio}\n" +
            "   • Película más antigua: {OldestMovie}\n" +
            "   • Película más reciente: {NewestMovie}",
            stats.TotalMovies, stats.MoviesThisYear, stats.AverageScore, stats.TopGenre,
            stats.TopStudio, 
            stats.OldestMovie != null ? $"{stats.OldestMovie.Film} ({stats.OldestMovie.Year})" : "N/A",
            stats.NewestMovie != null ? $"{stats.NewestMovie.Film} ({stats.NewestMovie.Year})" : "N/A");
    }

    /// <summary>
    /// Envía una alerta por error en la limpieza (implementación futura)
    /// </summary>
    private Task SendCleanupErrorAlert(Exception ex)
    {
        // Aquí podrías implementar envío de emails, Teams, Slack, etc.
        _logger.LogWarning("⚠️ Se debería enviar alerta por error en limpieza: {ErrorMessage}", ex.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Estadísticas de la base de datos
/// </summary>
public class DatabaseStats
{
    public int TotalMovies { get; set; }
    public int MoviesThisYear { get; set; }
    public double AverageScore { get; set; }
    public string TopGenre { get; set; } = string.Empty;
    public string TopStudio { get; set; } = string.Empty;
    public dynamic? OldestMovie { get; set; }
    public dynamic? NewestMovie { get; set; }
} 