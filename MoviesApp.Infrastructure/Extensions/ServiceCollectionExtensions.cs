using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoviesApp.Domain.Interfaces;
using MoviesApp.Infrastructure.Data;
using MoviesApp.Infrastructure.Repositories;

namespace MoviesApp.Infrastructure.Extensions;

/// <summary>
/// Extensiones para configurar servicios de infraestructura
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura Entity Framework y el DbContext
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar Entity Framework
        services.AddEntityFramework(configuration);

        // Configurar repositorios
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Configura Entity Framework con SQL Server
    /// </summary>
    public static IServiceCollection AddEntityFramework(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.AddDbContext<MoviesDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("MoviesApp.Infrastructure");
                sqlOptions.CommandTimeout(60);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

            // Configuraciones adicionales basadas en el entorno
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
            else
            {
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
            }

            options.EnableServiceProviderCaching();
        });

        return services;
    }

    /// <summary>
    /// Configura los repositorios
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Registrar el repositorio de películas
        services.AddScoped<IMovieRepository, MovieRepository>();
        
        return services;
    }

    /// <summary>
    /// Obtiene la cadena de conexión desde configuración
    /// </summary>
    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'DefaultConnection' en la configuración. " +
                "Asegúrate de que esté definida en appsettings.json o appsettings.Development.json. " +
                "Ejemplo: \"ConnectionStrings\": { \"DefaultConnection\": \"Server=...\" }");
        }

        return connectionString;
    }
} 