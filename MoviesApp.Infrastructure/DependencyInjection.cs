using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoviesApp.Domain.Interfaces;
using MoviesApp.Infrastructure.Data;
using MoviesApp.Infrastructure.Repositories;

namespace MoviesApp.Infrastructure;

/// <summary>
/// Extensión para configurar la inyección de dependencias de la capa de infraestructura
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de la capa de infraestructura al contenedor de dependencias
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <param name="configuration">Configuración de la aplicación</param>
    /// <returns>Colección de servicios configurada</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurar Entity Framework y SQL Server
        services.AddDbContext<MoviesDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Registrar repositorios
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
} 