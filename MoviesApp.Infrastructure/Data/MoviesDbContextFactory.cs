using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoviesApp.Infrastructure.Data;

/// <summary>
/// Factory para crear el DbContext en tiempo de diseño (migraciones)
/// </summary>
public class MoviesDbContextFactory : IDesignTimeDbContextFactory<MoviesDbContext>
{
    public MoviesDbContext CreateDbContext(string[] args)
    {
        // Para migraciones, se debe usar desde la aplicación principal con configuración
        throw new InvalidOperationException(
            "Este DbContextFactory está diseñado para ser usado desde la aplicación principal. " +
            "Para crear migraciones, ejecuta los comandos desde el proyecto MoviesApp.API que tiene acceso a la configuración. " +
            "Ejemplo: dotnet ef migrations add InitialCreate --project MoviesApp.Infrastructure --startup-project MoviesApp.API");
    }
} 