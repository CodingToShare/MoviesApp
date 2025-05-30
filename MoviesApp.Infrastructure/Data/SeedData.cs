using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoviesApp.Domain.Entities;

namespace MoviesApp.Infrastructure.Data;

/// <summary>
/// Clase para inicializar datos de prueba en la base de datos
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Inicializa la base de datos con datos de prueba
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MoviesDbContext>>();

        try
        {
            logger.LogInformation("🌱 Iniciando proceso de seeding de datos...");

            // Verificar conexión a la base de datos
            var canConnect = await context.Database.CanConnectAsync();
            logger.LogInformation("🔗 Conexión a BD: {CanConnect}", canConnect ? "✅ Exitosa" : "❌ Fallida");

            if (!canConnect)
            {
                logger.LogError("❌ No se puede conectar a la base de datos para hacer seeding");
                return;
            }

            // Inicializar usuarios si no existen
            await SeedUsersAsync(context, logger);

            // Inicializar películas de muestra si no existen
            await SeedMoviesAsync(context, logger);

            await context.SaveChangesAsync();
            logger.LogInformation("✅ Datos iniciales creados exitosamente");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error al inicializar datos");
            throw;
        }
    }

    private static async Task SeedUsersAsync(MoviesDbContext context, ILogger logger)
    {
        logger.LogInformation("👤 Verificando usuarios existentes...");
        
        var existingUsersCount = await context.Users.CountAsync();
        logger.LogInformation("👤 Usuarios existentes en BD: {Count}", existingUsersCount);

        if (existingUsersCount > 0)
        {
            logger.LogInformation("👤 Los usuarios ya existen, omitiendo seeding de usuarios");
            return;
        }

        logger.LogInformation("👤 Creando usuarios de prueba...");

        var users = new[]
        {
            new User
            {
                Username = "admin",
                Email = "admin@moviesapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPass123", 12),
                FirstName = "Admin",
                LastName = "User",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "testuser",
                Email = "testuser@moviesapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123", 12),
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "demo",
                Email = "demo@moviesapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123", 12),
                FirstName = "Demo",
                LastName = "User",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        logger.LogInformation("👤 ✅ Usuarios de prueba agregados: {Count}", users.Length);
    }

    private static async Task SeedMoviesAsync(MoviesDbContext context, ILogger logger)
    {
        logger.LogInformation("🎬 Verificando películas existentes...");
        
        var existingMoviesCount = await context.Movies.CountAsync();
        logger.LogInformation("🎬 Películas existentes en BD: {Count}", existingMoviesCount);

        if (existingMoviesCount > 0)
        {
            logger.LogInformation("🎬 Las películas ya existen, omitiendo seeding de películas");
            return;
        }

        logger.LogInformation("🎬 Creando películas de muestra...");

        var movies = new[]
        {
            new Movie(1, "The Avengers", "Action", "Marvel Studios", 85, 2012),
            new Movie(2, "Inception", "Sci-Fi", "Warner Bros", 92, 2010),
            new Movie(3, "The Dark Knight", "Action", "Warner Bros", 94, 2008),
            new Movie(4, "Pulp Fiction", "Crime", "Miramax", 89, 1994),
            new Movie(5, "Forrest Gump", "Drama", "Paramount Pictures", 88, 1994),
            new Movie(6, "The Matrix", "Sci-Fi", "Warner Bros", 87, 1999),
            new Movie(7, "Goodfellas", "Crime", "Warner Bros", 90, 1990),
            new Movie(8, "The Godfather", "Crime", "Paramount Pictures", 95, 1972),
            new Movie(9, "Casablanca", "Romance", "Warner Bros", 91, 1942),
            new Movie(10, "Titanic", "Romance", "20th Century Fox", 86, 1997)
        };

        await context.Movies.AddRangeAsync(movies);
        logger.LogInformation("🎬 ✅ Películas de muestra agregadas: {Count}", movies.Length);
    }
} 