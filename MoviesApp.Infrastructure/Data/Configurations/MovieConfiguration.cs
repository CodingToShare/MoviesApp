using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoviesApp.Domain.Entities;

namespace MoviesApp.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración específica para la entidad Movie
/// </summary>
public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        // Configuración de la tabla con restricciones
        builder.ToTable("Movies", schema: "dbo", t =>
        {
            // Restricción de puntuación válida (0-100)
            t.HasCheckConstraint("CK_Movies_Score_Range", "[Score] >= 0 AND [Score] <= 100");
            
            // Restricción de año válido (1900-2100)
            t.HasCheckConstraint("CK_Movies_Year_Range", "[Year] >= 1900 AND [Year] <= 2100");
            
            // Restricción de ID de negocio positivo
            t.HasCheckConstraint("CK_Movies_Id_Positive", "[Id] > 0");
            
            // Restricción de longitud mínima para campos de texto
            t.HasCheckConstraint("CK_Movies_Film_NotEmpty", "LEN(TRIM([Film])) > 0");
            t.HasCheckConstraint("CK_Movies_Genre_NotEmpty", "LEN(TRIM([Genre])) > 0");
            t.HasCheckConstraint("CK_Movies_Studio_NotEmpty", "LEN(TRIM([Studio])) > 0");
            
            // Restricción de fechas lógicas
            t.HasCheckConstraint("CK_Movies_UpdatedAt_AfterCreatedAt", "[UpdatedAt] IS NULL OR [UpdatedAt] >= [CreatedAt]");
        });

        // Configuración de la clave primaria
        builder.HasKey(m => m.MovieId);

        // Configuración de propiedades
        ConfigureProperties(builder);

        // Configuración de índices
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configuración de las propiedades de la entidad
    /// </summary>
    private static void ConfigureProperties(EntityTypeBuilder<Movie> builder)
    {
        // MovieId - Clave primaria
        builder.Property(m => m.MovieId)
            .IsRequired()
            .HasDefaultValueSql("NEWID()")
            .HasComment("Identificador único de la tabla (Primary Key)");

        // Id - Identificador de negocio
        builder.Property(m => m.Id)
            .IsRequired()
            .HasColumnName("Id")
            .HasColumnType("int")
            .HasComment("Identificador único de la película (Negocio)");

        // Film - Nombre de la película
        builder.Property(m => m.Film)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("nvarchar(255)")
            .HasComment("Nombre de la película");

        // Genre - Género
        builder.Property(m => m.Genre)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)")
            .HasComment("Género de la película");

        // Studio - Estudio
        builder.Property(m => m.Studio)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnType("nvarchar(150)")
            .HasComment("Estudio que produjo la película");

        // Score - Puntuación
        builder.Property(m => m.Score)
            .IsRequired()
            .HasColumnType("int")
            .HasComment("Puntuación de la audiencia (0-100)");

        // Year - Año
        builder.Property(m => m.Year)
            .IsRequired()
            .HasColumnType("int")
            .HasComment("Año de estreno de la película");

        // CreatedAt - Fecha de creación
        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("GETUTCDATE()")
            .HasComment("Fecha de creación del registro");

        // UpdatedAt - Fecha de actualización
        builder.Property(m => m.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .HasComment("Fecha de última actualización del registro");
    }

    /// <summary>
    /// Configuración de índices para optimización de consultas
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<Movie> builder)
    {
        // Índice único en el ID de negocio
        builder.HasIndex(m => m.Id)
            .IsUnique()
            .HasDatabaseName("IX_Movies_Id")
            .HasFilter(null); // Sin filtro para que sea aplicable a todos los registros

        // Índice en el nombre de la película para búsquedas de texto
        builder.HasIndex(m => m.Film)
            .HasDatabaseName("IX_Movies_Film")
            .HasFilter("[Film] IS NOT NULL");

        // Índice en el género para filtros frecuentes
        builder.HasIndex(m => m.Genre)
            .HasDatabaseName("IX_Movies_Genre")
            .HasFilter("[Genre] IS NOT NULL");

        // Índice en el estudio para filtros
        builder.HasIndex(m => m.Studio)
            .HasDatabaseName("IX_Movies_Studio")
            .HasFilter("[Studio] IS NOT NULL");

        // Índice en el año para filtros y ordenamiento
        builder.HasIndex(m => m.Year)
            .HasDatabaseName("IX_Movies_Year");

        // Índice en la puntuación para filtros y ordenamiento
        builder.HasIndex(m => m.Score)
            .HasDatabaseName("IX_Movies_Score");

        // Índice compuesto para consultas comunes (género + año)
        builder.HasIndex(m => new { m.Genre, m.Year })
            .HasDatabaseName("IX_Movies_Genre_Year")
            .HasFilter("[Genre] IS NOT NULL");

        // Índice compuesto para consultas de puntuación por año
        builder.HasIndex(m => new { m.Year, m.Score })
            .HasDatabaseName("IX_Movies_Year_Score");

        // Índice en fecha de creación para auditoría y consultas temporales
        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("IX_Movies_CreatedAt");

        // Índice en fecha de actualización para auditoría
        builder.HasIndex(m => m.UpdatedAt)
            .HasDatabaseName("IX_Movies_UpdatedAt")
            .HasFilter("[UpdatedAt] IS NOT NULL");
    }
} 