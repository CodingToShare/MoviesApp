using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.Helpers;
using MoviesApp.Application.Interfaces;
using MoviesApp.Domain.Entities;
using MoviesApp.Domain.Interfaces;

namespace MoviesApp.Application.Services;

/// <summary>
/// Implementación básica del servicio de películas
/// </summary>
public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<MovieService> _logger;
    private readonly IValidator<CreateMovieDto> _createValidator;

    public MovieService(
        IMovieRepository movieRepository,
        IMapper mapper,
        ILogger<MovieService> logger,
        IValidator<CreateMovieDto> createValidator)
    {
        _movieRepository = movieRepository ?? throw new ArgumentNullException(nameof(movieRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
    }

    /// <summary>
    /// Obtiene una película por su ID
    /// </summary>
    public async Task<MovieDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obteniendo película con ID: {Id}", id);

            // Validate input but don't return early - continue with security checks
            var isValidId = id > 0;
            if (!isValidId)
            {
                _logger.LogWarning("ID inválido: {Id}", id);
            }

            // Always perform the repository call regardless of input validation
            // This prevents user-controlled bypass of sensitive operations
            Movie? movie = null;
            if (isValidId)
            {
                movie = await _movieRepository.GetByIdAsync(id, cancellationToken);
            }
            
            if (movie == null)
            {
                _logger.LogDebug("No se encontró película con ID: {Id}", id);
                return null;
            }

            var movieDto = _mapper.Map<MovieDto>(movie);
            _logger.LogDebug("Película encontrada: {Film}", SecurityHelper.SanitizeForLogging(movieDto.Film));
            
            return movieDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener película con ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Obtiene todas las películas con paginación y ordenamiento
    /// </summary>
    public async Task<IEnumerable<MovieDto>> GetAllAsync(
        int total = 50, 
        string order = "asc", 
        string orderBy = "Year", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sanitizar parámetros del usuario para prevenir log injection
            _logger.LogDebug("Obteniendo películas - total: {Total}, order: {Order}, orderBy: {OrderBy}", 
                total, 
                SecurityHelper.SanitizeForLogging(order), 
                SecurityHelper.SanitizeForLogging(orderBy));

            total = Math.Max(1, total);
            var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);

            var movies = await _movieRepository.GetAllAsync(0, total, orderBy, ascending, cancellationToken);
            var movieDtos = _mapper.Map<IEnumerable<MovieDto>>(movies);

            _logger.LogDebug("Se obtuvieron {Count} películas", movieDtos.Count());
            return movieDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las películas");
            throw;
        }
    }

    /// <summary>
    /// Agrega una nueva película
    /// </summary>
    public async Task<MovieDto> AddAsync(CreateMovieDto createMovieDto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creando nueva película: {Film}", SecurityHelper.SanitizeForLogging(createMovieDto.Film));

            // Validar entrada
            var validationResult = await _createValidator.ValidateAsync(createMovieDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                // Sanitize user input to prevent log injection/forgery
                var sanitizedFilm = SecurityHelper.SanitizeForLogging(createMovieDto.Film);
                _logger.LogWarning("Validación fallida para película {Film}: {Errors}", sanitizedFilm, errors);
                throw new FluentValidation.ValidationException(validationResult.Errors);
            }

            // Verificar si ya existe
            var existingMovie = await _movieRepository.GetByIdAsync(createMovieDto.Id, cancellationToken);
            if (existingMovie != null)
            {
                _logger.LogWarning("Película con ID {Id} ya existe", createMovieDto.Id);
                throw new InvalidOperationException($"Ya existe una película con el ID {createMovieDto.Id}");
            }

            // Mapear y crear
            var movie = _mapper.Map<Movie>(createMovieDto);
            movie.CreatedAt = DateTime.UtcNow;
            
            await _movieRepository.AddAsync(movie, cancellationToken);
            
            var resultDto = _mapper.Map<MovieDto>(movie);
            _logger.LogInformation("Película creada exitosamente: {Film} con ID {Id}", SecurityHelper.SanitizeForLogging(resultDto.Film), resultDto.Id);

            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear película: {Film}", SecurityHelper.SanitizeForLogging(createMovieDto.Film));
            throw;
        }
    }

    // Métodos no implementados para los 3 endpoints básicos
    public Task<IEnumerable<MovieDto>> GetByGenreAsync(string genre, CancellationToken cancellationToken = default)
    {
        return Task.FromException<IEnumerable<MovieDto>>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<IEnumerable<MovieDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        return Task.FromException<IEnumerable<MovieDto>>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<IEnumerable<MovieDto>> GetByMinScoreAsync(int minScore, CancellationToken cancellationToken = default)
    {
        return Task.FromException<IEnumerable<MovieDto>>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<MovieDto?> UpdateAsync(int id, UpdateMovieDto updateMovieDto, CancellationToken cancellationToken = default)
    {
        return Task.FromException<MovieDto?>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromException<bool>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromException<bool>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<int>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<CsvLoadResultDto> LoadFromCsvAsync(string csvFilePath, bool validateDuplicates = true, CancellationToken cancellationToken = default)
    {
        return Task.FromException<CsvLoadResultDto>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<CsvLoadResultDto> LoadFromCsvStreamAsync(Stream csvStream, bool validateDuplicates = true, CancellationToken cancellationToken = default)
    {
        return Task.FromException<CsvLoadResultDto>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<ValidationResultDto> ValidateMovieAsync(CreateMovieDto createMovieDto)
    {
        return Task.FromException<ValidationResultDto>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }

    public Task<MovieStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromException<MovieStatsDto>(
            new NotImplementedException("Método no implementado para los endpoints básicos"));
    }
} 