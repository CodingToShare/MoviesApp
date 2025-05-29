using MoviesApp.Domain.Entities;

namespace MoviesApp.Domain.Interfaces;

/// <summary>
/// Contrato para el repositorio de películas
/// Define las operaciones de acceso a datos para la entidad Movie
/// </summary>
public interface IMovieRepository
{
    /// <summary>
    /// Obtiene una película por su ID de negocio
    /// </summary>
    /// <param name="id">ID de negocio de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>La película encontrada o null si no existe</returns>
    Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una película por su MovieId (Guid)
    /// </summary>
    /// <param name="movieId">MovieId único de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>La película encontrada o null si no existe</returns>
    Task<Movie?> GetByMovieIdAsync(Guid movieId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las películas con paginación y ordenamiento
    /// </summary>
    /// <param name="skip">Número de registros a omitir</param>
    /// <param name="take">Número de registros a tomar</param>
    /// <param name="orderBy">Campo por el cual ordenar</param>
    /// <param name="ascending">Indica si el orden es ascendente</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas ordenadas</returns>
    Task<IEnumerable<Movie>> GetAllAsync(
        int skip = 0, 
        int take = int.MaxValue, 
        string orderBy = "Year", 
        bool ascending = true, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas filtradas por género
    /// </summary>
    /// <param name="genre">Género a filtrar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas del género especificado</returns>
    Task<IEnumerable<Movie>> GetByGenreAsync(string genre, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas filtradas por año
    /// </summary>
    /// <param name="year">Año a filtrar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas del año especificado</returns>
    Task<IEnumerable<Movie>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas con puntuación mayor o igual a la especificada
    /// </summary>
    /// <param name="minScore">Puntuación mínima</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas con la puntuación especificada o mayor</returns>
    Task<IEnumerable<Movie>> GetByMinScoreAsync(int minScore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva película
    /// </summary>
    /// <param name="movie">Película a agregar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>La película agregada</returns>
    Task<Movie> AddAsync(Movie movie, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega múltiples películas en una operación batch
    /// </summary>
    /// <param name="movies">Lista de películas a agregar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas agregadas</returns>
    Task<IEnumerable<Movie>> AddRangeAsync(IEnumerable<Movie> movies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una película existente
    /// </summary>
    /// <param name="movie">Película a actualizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>La película actualizada</returns>
    Task<Movie> UpdateAsync(Movie movie, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina una película por su ID de negocio
    /// </summary>
    /// <param name="id">ID de negocio de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si se eliminó correctamente, false si no se encontró</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe una película con el ID de negocio especificado
    /// </summary>
    /// <param name="id">ID de negocio de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si existe, false si no existe</returns>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el conteo total de películas
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Número total de películas</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga películas desde un archivo CSV
    /// </summary>
    /// <param name="csvFilePath">Ruta del archivo CSV</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas cargadas desde el CSV</returns>
    Task<IEnumerable<Movie>> LoadFromCsvAsync(string csvFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga películas desde un stream CSV
    /// </summary>
    /// <param name="csvStream">Stream del archivo CSV</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas cargadas desde el CSV</returns>
    Task<IEnumerable<Movie>> LoadFromCsvStreamAsync(Stream csvStream, CancellationToken cancellationToken = default);
} 