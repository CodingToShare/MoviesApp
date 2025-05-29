using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoviesApp.Functions.Helpers;
using MoviesApp.Infrastructure.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configurar Entity Framework
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection") 
                              ?? context.Configuration["SqlConnectionString"];
        
        services.AddDbContext<MoviesDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Registrar servicios personalizados
        services.AddScoped<CsvProcessingService>();

        // Configurar logging
        services.AddLogging();
    })
    .Build();

// ✅ Ejecutar migraciones automáticamente al iniciar Azure Functions
using (var scope = host.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("🔄 [Azure Functions] Verificando migraciones pendientes...");
        
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("📝 [Azure Functions] Se encontraron {Count} migraciones pendientes: {Migrations}", 
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                
            logger.LogInformation("⚙️ [Azure Functions] Ejecutando migraciones automáticamente...");
            await context.Database.MigrateAsync();
            logger.LogInformation("✅ [Azure Functions] Migraciones ejecutadas exitosamente");
        }
        else
        {
            logger.LogInformation("✅ [Azure Functions] Base de datos actualizada - No hay migraciones pendientes");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ [Azure Functions] Error al ejecutar migraciones automáticamente");
        throw; // Re-lanzar la excepción para que las Functions no inicien con problemas de BD
    }
}

host.Run();
