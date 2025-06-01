using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApp.Domain.Entities;
using MoviesApp.Domain.Interfaces;
using MoviesApp.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace MoviesApp.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de películas usando Entity Framework
/// </summary>
public class MovieRepository : IMovieRepository
{
    private readonly MoviesDbContext _context;
    private readonly ILogger<MovieRepository> _logger;

    public MovieRepository(MoviesDbContext context, ILogger<MovieRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Consultas

    /// <summary>
    /// Obtiene una película por su ID de negocio
    /// </summary>
    public async Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando película con ID: {Id}", id);
            
            var movie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (movie != null)
            {
                _logger.LogDebug("Película encontrada: {Film} ({Year})", movie.Film, movie.Year);
            }
            else
            {
                _logger.LogDebug("No se encontró película con ID: {Id}", id);
            }

            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar película con ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene una película por su MovieId (Guid)
    /// </summary>
    public async Task<Movie?> GetByMovieIdAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando película con GUID: {MovieId}", movieId);
            
            return await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MovieId == movieId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar película con GUID: {MovieId}", movieId);
            throw;
        }
    }

    /// <summary>
    /// Obtiene todas las películas con paginación y ordenamiento
    /// </summary>
    public async Task<IEnumerable<Movie>> GetAllAsync(int skip = 0, int take = int.MaxValue, string orderBy = "Year", bool ascending = true, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obteniendo películas: skip={Skip}, take={Take}, orderBy={OrderBy}, ascending={Ascending}", 
                skip, take, orderBy, ascending);

            var query = _context.Movies.AsNoTracking();

            // Aplicar ordenamiento
            query = ApplyOrdering(query, orderBy, ascending);

            // Aplicar paginación
            var movies = await query
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Se obtuvieron {Count} películas", movies.Count);
            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener películas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene películas por género
    /// </summary>
    public async Task<IEnumerable<Movie>> GetByGenreAsync(string genre, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar y sanitizar input para prevenir inyección SQL
            if (string.IsNullOrWhiteSpace(genre))
            {
                _logger.LogWarning("Se intentó buscar películas con género nulo o vacío");
                return Enumerable.Empty<Movie>();
            }

            // Sanitizar el género para prevenir ataques de inyección
            var sanitizedGenre = SanitizeSearchInput(genre);
            if (string.IsNullOrWhiteSpace(sanitizedGenre))
            {
                _logger.LogWarning("Género contiene caracteres no válidos: {Genre}", genre);
                return Enumerable.Empty<Movie>();
            }

            _logger.LogDebug("Buscando películas del género: {Genre}", sanitizedGenre);
            
            // Usar EF Core con parámetros seguros (protege contra SQL injection)
            var movies = await _context.Movies
                .AsNoTracking()
                .Where(m => EF.Functions.Like(m.Genre, $"%{sanitizedGenre}%"))
                .OrderBy(m => m.Film)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Se encontraron {Count} películas del género {Genre}", movies.Count, sanitizedGenre);
            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar películas por género: {Genre}", genre);
            throw;
        }
    }

    /// <summary>
    /// Obtiene películas por año
    /// </summary>
    public async Task<IEnumerable<Movie>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando películas del año: {Year}", year);
            
            var movies = await _context.Movies
                .AsNoTracking()
                .Where(m => m.Year == year)
                .OrderByDescending(m => m.Score)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Se encontraron {Count} películas del año {Year}", movies.Count, year);
            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar películas por año: {Year}", year);
            throw;
        }
    }

    /// <summary>
    /// Obtiene películas con puntuación mínima
    /// </summary>
    public async Task<IEnumerable<Movie>> GetByMinScoreAsync(int minScore, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando películas con puntuación mínima: {MinScore}", minScore);
            
            var movies = await _context.Movies
                .AsNoTracking()
                .Where(m => m.Score >= minScore)
                .OrderByDescending(m => m.Score)
                .ThenBy(m => m.Film)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Se encontraron {Count} películas con puntuación >= {MinScore}", movies.Count, minScore);
            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar películas por puntuación mínima: {MinScore}", minScore);
            throw;
        }
    }

    #endregion

    #region Operaciones de Escritura

    /// <summary>
    /// Agrega una nueva película
    /// </summary>
    public async Task<Movie> AddAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Agregando nueva película: {Film} ({Year})", movie.Film, movie.Year);

            // Verificar que no exista una película con el mismo ID de negocio
            var existingMovie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == movie.Id, cancellationToken);

            if (existingMovie != null)
            {
                throw new InvalidOperationException($"Ya existe una película con el ID {movie.Id}");
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Película agregada exitosamente: {Film} con ID {Id}", movie.Film, movie.Id);
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar película: {Film}", movie.Film);
            throw;
        }
    }

    /// <summary>
    /// Agrega múltiples películas en lote
    /// </summary>
    public async Task<IEnumerable<Movie>> AddRangeAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken = default)
    {
        try
        {
            var movieList = movies.ToList();
            _logger.LogDebug("Agregando {Count} películas en lote", movieList.Count);

            // Verificar duplicados por ID de negocio
            var ids = movieList.Select(m => m.Id).ToList();
            var existingIds = await _context.Movies
                .Where(m => ids.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (existingIds.Any())
            {
                throw new InvalidOperationException($"Ya existen películas con los IDs: {string.Join(", ", existingIds)}");
            }

            _context.Movies.AddRange(movieList);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Se agregaron {Count} películas exitosamente", movieList.Count);
            return movieList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar películas en lote");
            throw;
        }
    }

    /// <summary>
    /// Actualiza una película existente
    /// </summary>
    public async Task<Movie> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Actualizando película con ID: {Id}", movie.Id);

            var existingMovie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == movie.Id, cancellationToken);

            if (existingMovie == null)
            {
                throw new InvalidOperationException($"No se encontró la película con ID {movie.Id}");
            }

            // Actualizar propiedades
            existingMovie.Film = movie.Film;
            existingMovie.Genre = movie.Genre;
            existingMovie.Studio = movie.Studio;
            existingMovie.Score = movie.Score;
            existingMovie.Year = movie.Year;
            existingMovie.MarkAsUpdated();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Película actualizada exitosamente: {Film} con ID {Id}", movie.Film, movie.Id);
            return existingMovie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar película con ID: {Id}", movie.Id);
            throw;
        }
    }

    /// <summary>
    /// Elimina una película por ID
    /// </summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Eliminando película con ID: {Id}", id);

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (movie == null)
            {
                _logger.LogWarning("No se encontró película con ID {Id} para eliminar", id);
                return false;
            }

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Película eliminada exitosamente: {Film} con ID {Id}", movie.Film, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar película con ID: {Id}", id);
            throw;
        }
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Verifica si existe una película con el ID especificado
    /// </summary>
    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Movies
                .AnyAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar existencia de película con ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el conteo total de películas
    /// </summary>
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Movies.CountAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de operación al obtener conteo de películas");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Operación cancelada al obtener conteo de películas");
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout al obtener conteo de películas");
            throw;
        }
    }

    #endregion

    #region Carga CSV

    /// <summary>
    /// Carga películas desde un archivo CSV
    /// </summary>
    public async Task<IEnumerable<Movie>> LoadFromCsvAsync(string csvFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cargando películas desde archivo CSV: {FilePath}", csvFilePath);

            if (!File.Exists(csvFilePath))
            {
                throw new FileNotFoundException($"No se encontró el archivo CSV: {csvFilePath}");
            }

            using var fileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read);
            return await LoadFromCsvStreamAsync(fileStream, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Archivo CSV no encontrado: {FilePath}", csvFilePath);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Error de permisos al cargar películas desde CSV: {FilePath}", csvFilePath);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error de E/S al cargar películas desde CSV: {FilePath}", csvFilePath);
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argumento inválido al cargar películas desde CSV: {FilePath}", csvFilePath);
            throw;
        }
    }

    /// <summary>
    /// Carga películas desde un stream CSV
    /// </summary>
    public Task<IEnumerable<Movie>> LoadFromCsvStreamAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Cargando películas desde stream CSV");

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            // Configurar mapeo de CSV sin usar ClassMap para evitar virtual calls en constructor
            ConfigureMovieCsvMapping(csv);
            
            var movies = new List<Movie>();
            var records = csv.GetRecords<MovieCsvRecord>();

            foreach (var record in records)
            {
                // Verificar cancelación
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var movie = new Movie
                    {
                        Id = record.Id,
                        Film = record.Film?.Trim() ?? string.Empty,
                        Genre = record.Genre?.Trim() ?? string.Empty,
                        Studio = record.Studio?.Trim() ?? string.Empty,
                        Score = record.Score,
                        Year = record.Year
                    };

                    if (movie.IsValid())
                    {
                        movies.Add(movie);
                    }
                    else
                    {
                        _logger.LogWarning("Película inválida en CSV: ID={Id}, Film={Film}", record.Id, record.Film);
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Error de argumento al procesar registro CSV: ID={Id}, Film={Film}", record.Id, record.Film);
                }
                catch (FormatException ex)
                {
                    _logger.LogWarning(ex, "Error de formato al procesar registro CSV: ID={Id}, Film={Film}", record.Id, record.Film);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Error de operación al procesar registro CSV: ID={Id}, Film={Film}", record.Id, record.Film);
                }
            }

            _logger.LogInformation("Se cargaron {Count} películas válidas desde CSV", movies.Count);
            return Task.FromResult<IEnumerable<Movie>>(movies);
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError(ex, "Error de CsvHelper al cargar películas desde stream CSV");
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error de E/S al cargar películas desde stream CSV");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Error de permisos al cargar películas desde stream CSV");
            throw;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argumento inválido al cargar películas desde stream CSV");
            throw;
        }
    }

    #endregion

    #region Métodos Privados

    /// <summary>
    /// Configura el mapeo CSV sin usar herencia para evitar virtual calls en constructor
    /// </summary>
    private static void ConfigureMovieCsvMapping(CsvReader csv)
    {
        // CsvHelper usando atributos no requiere configuración adicional
        // Los atributos [Name] en MovieCsvRecord manejan el mapeo automáticamente
    }

    /// <summary>
    /// Aplica ordenamiento a la consulta
    /// </summary>
    private static IQueryable<Movie> ApplyOrdering(IQueryable<Movie> query, string orderBy, bool ascending)
    {
        return orderBy.ToLowerInvariant() switch
        {
            "id" => ascending ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id),
            "film" => ascending ? query.OrderBy(m => m.Film) : query.OrderByDescending(m => m.Film),
            "genre" => ascending ? query.OrderBy(m => m.Genre) : query.OrderByDescending(m => m.Genre),
            "studio" => ascending ? query.OrderBy(m => m.Studio) : query.OrderByDescending(m => m.Studio),
            "score" => ascending ? query.OrderBy(m => m.Score) : query.OrderByDescending(m => m.Score),
            "year" => ascending ? query.OrderBy(m => m.Year) : query.OrderByDescending(m => m.Year),
            "createdat" => ascending ? query.OrderBy(m => m.CreatedAt) : query.OrderByDescending(m => m.CreatedAt),
            "updatedat" => ascending ? query.OrderBy(m => m.UpdatedAt) : query.OrderByDescending(m => m.UpdatedAt),
            _ => ascending ? query.OrderBy(m => m.Year) : query.OrderByDescending(m => m.Year)
        };
    }

    /// <summary>
    /// Sanitiza la entrada de búsqueda para prevenir inyecciones SQL y XSS
    /// </summary>
    private static string SanitizeSearchInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remover caracteres peligrosos que podrían usarse en ataques de inyección
        var dangerousChars = new[] { 
            "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", 
            "--", "/*", "*/", "xp_", "sp_", "exec", "execute",
            "drop", "delete", "insert", "update", "create", "alter", "union", "select"
        };

        var sanitized = input;

        foreach (var dangerousChar in dangerousChars)
        {
            sanitized = sanitized.Replace(dangerousChar, "", StringComparison.OrdinalIgnoreCase);
        }

        // Limitar longitud para prevenir ataques de buffer overflow
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100];
        }

        return sanitized.Trim();
    }

    #endregion
}

/// <summary>
/// Clase para mapear registros CSV usando atributos en lugar de ClassMap
/// para evitar virtual calls en constructor
/// </summary>
public class MovieCsvRecord
{
    [Name("ID", "Id", "id")]
    public int Id { get; set; }
    
    [Name("Film", "Movie", "Title", "film", "movie", "title")]
    public string? Film { get; set; }
    
    [Name("Genre", "genre")]
    public string? Genre { get; set; }
    
    [Name("Studio", "studio")]
    public string? Studio { get; set; }
    
    [Name("Score", "Rating", "score", "rating")]
    public int Score { get; set; }
    
    [Name("Year", "year")]
    public int Year { get; set; }
} 