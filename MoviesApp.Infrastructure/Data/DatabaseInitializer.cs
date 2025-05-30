using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MoviesApp.Infrastructure.Data;

/// <summary>
/// Inicializador de base de datos para configuración y creación automática
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Inicializa la base de datos creando las tablas y datos iniciales
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MoviesDbContext>>();

        try
        {
            // Crear la base de datos si no existe
            await context.Database.EnsureCreatedAsync();
            
            logger.LogInformation("Base de datos inicializada correctamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    /// <summary>
    /// Aplica las migraciones pendientes
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MoviesDbContext>>();

        try
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Aplicando {Count} migraciones pendientes", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Migraciones aplicadas correctamente");
            }
            else
            {
                logger.LogInformation("No hay migraciones pendientes");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al aplicar migraciones");
            throw;
        }
    }

    /// <summary>
    /// Verifica la conectividad con la base de datos
    /// </summary>
    public static async Task<bool> CanConnectAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MoviesDbContext>>();

        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
            {
                logger.LogInformation("Conexión a la base de datos exitosa");
            }
            else
            {
                logger.LogWarning("No se pudo conectar a la base de datos");
            }
            
            return canConnect;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar la conexión a la base de datos");
            return false;
        }
    }

    /// <summary>
    /// Obtiene información sobre el estado de la base de datos
    /// </summary>
    public static async Task<DatabaseInfo> GetDatabaseInfoAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();

        var info = new DatabaseInfo
        {
            DatabaseExists = await context.Database.CanConnectAsync(),
            PendingMigrations = await context.Database.GetPendingMigrationsAsync(),
            AppliedMigrations = await context.Database.GetAppliedMigrationsAsync(),
            MovieCount = await context.Movies.CountAsync()
        };

        return info;
    }
}

/// <summary>
/// Información sobre el estado de la base de datos
/// </summary>
public class DatabaseInfo
{
    public bool DatabaseExists { get; set; }
    public IEnumerable<string> PendingMigrations { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> AppliedMigrations { get; set; } = Enumerable.Empty<string>();
    public int MovieCount { get; set; }
} 