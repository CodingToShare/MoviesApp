using MoviesApp.Application.DTOs;

namespace MoviesApp.Application.Interfaces;

/// <summary>
/// Contrato para el servicio de películas
/// Define la lógica de negocio para el manejo de películas
/// </summary>
public interface IMovieService
{
    /// <summary>
    /// Obtiene una película por su ID de negocio
    /// </summary>
    /// <param name="id">ID de negocio de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DTO de la película encontrada o null si no existe</returns>
    Task<MovieDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las películas con paginación y ordenamiento
    /// </summary>
    /// <param name="total">Número máximo de películas a retornar</param>
    /// <param name="order">Orden: "asc" para ascendente, "desc" para descendente</param>
    /// <param name="orderBy">Campo por el cual ordenar (Year, Score, Film, etc.)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de DTOs de películas ordenadas</returns>
    Task<IEnumerable<MovieDto>> GetAllAsync(
        int total = int.MaxValue, 
        string order = "asc", 
        string orderBy = "Year", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas filtradas por género
    /// </summary>
    /// <param name="genre">Género a filtrar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de DTOs de películas del género especificado</returns>
    Task<IEnumerable<MovieDto>> GetByGenreAsync(string genre, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas filtradas por año
    /// </summary>
    /// <param name="year">Año a filtrar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de DTOs de películas del año especificado</returns>
    Task<IEnumerable<MovieDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene películas con puntuación mayor o igual a la especificada
    /// </summary>
    /// <param name="minScore">Puntuación mínima</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de DTOs de películas con la puntuación especificada o mayor</returns>
    Task<IEnumerable<MovieDto>> GetByMinScoreAsync(int minScore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva película
    /// </summary>
    /// <param name="createMovieDto">DTO con los datos de la película a crear</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DTO de la película creada</returns>
    Task<MovieDto> AddAsync(CreateMovieDto createMovieDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una película existente
    /// </summary>
    /// <param name="id">ID de negocio de la película a actualizar</param>
    /// <param name="updateMovieDto">DTO con los datos actualizados</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DTO de la película actualizada o null si no se encontró</returns>
    Task<MovieDto?> UpdateAsync(int id, UpdateMovieDto updateMovieDto, CancellationToken cancellationToken = default);

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
    /// <param name="validateDuplicates">Indica si se deben validar duplicados</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de carga</returns>
    Task<CsvLoadResultDto> LoadFromCsvAsync(string csvFilePath, bool validateDuplicates = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga películas desde un stream CSV
    /// </summary>
    /// <param name="csvStream">Stream del archivo CSV</param>
    /// <param name="validateDuplicates">Indica si se deben validar duplicados</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado de la operación de carga</returns>
    Task<CsvLoadResultDto> LoadFromCsvStreamAsync(Stream csvStream, bool validateDuplicates = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida los datos de una película
    /// </summary>
    /// <param name="createMovieDto">DTO con los datos a validar</param>
    /// <returns>Resultado de la validación</returns>
    Task<ValidationResultDto> ValidateMovieAsync(CreateMovieDto createMovieDto);

    /// <summary>
    /// Obtiene estadísticas de las películas
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DTO con estadísticas de las películas</returns>
    Task<MovieStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
} 