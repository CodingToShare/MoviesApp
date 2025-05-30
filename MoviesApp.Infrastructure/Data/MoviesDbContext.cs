using Microsoft.EntityFrameworkCore;
using MoviesApp.Domain.Entities;
using MoviesApp.Infrastructure.Data.Configurations;

namespace MoviesApp.Infrastructure.Data;

/// <summary>
/// Contexto de base de datos para la aplicación de películas
/// </summary>
public class MoviesDbContext : DbContext
{
    /// <summary>
    /// Constructor que recibe las opciones de configuración
    /// </summary>
    public MoviesDbContext(DbContextOptions<MoviesDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet para las películas
    /// </summary>
    public DbSet<Movie> Movies { get; set; } = null!;

    /// <summary>
    /// DbSet para los usuarios
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Configuración del modelo usando Fluent API
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas las configuraciones de entidades
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MovieConfiguration).Assembly);

        // Configuración de índices para Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }

    /// <summary>
    /// Override SaveChanges para auditoría automática
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync para auditoría automática
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Actualiza automáticamente los campos de auditoría
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Movie>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.MarkAsUpdated();
        }
    }
} 