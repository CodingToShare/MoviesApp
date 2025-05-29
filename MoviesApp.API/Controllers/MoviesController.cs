using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.Interfaces;
using FluentValidation;
using System.Net;

namespace MoviesApp.API.Controllers;

/// <summary>
/// Controlador para la gestión básica de películas
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[Authorize]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(IMovieService movieService, ILogger<MoviesController> logger)
    {
        _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene una película por ID
    /// </summary>
    /// <param name="id">ID de la película</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Película encontrada</returns>
    /// <response code="200">Película encontrada exitosamente</response>
    /// <response code="400">ID inválido o parámetros incorrectos</response>
    /// <response code="401">No autorizado - Token JWT requerido</response>
    /// <response code="404">Película no encontrada</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("movie")]
    [ProducesResponseType(typeof(MovieDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<MovieDto>> GetMovie([FromQuery] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obteniendo película con ID: {Id}", id);

            // Validación de entrada
            if (id <= 0)
            {
                _logger.LogWarning("ID inválido proporcionado: {Id}", id);
                return BadRequest(new
                {
                    title = "ID inválido",
                    status = 400,
                    detail = $"El ID {id} no es válido. Debe ser un número positivo mayor a 0",
                    timestamp = DateTime.UtcNow
                });
            }

            var movie = await _movieService.GetByIdAsync(id, cancellationToken);
            
            if (movie == null)
            {
                _logger.LogInformation("Película con ID {Id} no encontrada", id);
                return NotFound(new
                {
                    title = "Película no encontrada",
                    status = 404,
                    detail = $"No se encontró una película con el ID {id}",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Película obtenida exitosamente: {Film} (ID: {Id})", movie.Film, id);
            return Ok(movie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener película con ID: {Id}", id);
            // El middleware de manejo de errores se encargará de esto
            throw;
        }
    }

    /// <summary>
    /// Obtiene todas las películas con paginación y ordenamiento
    /// </summary>
    /// <param name="total">Número máximo de películas a retornar (1-1000)</param>
    /// <param name="order">Orden: 'asc' o 'desc'</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de películas ordenadas</returns>
    /// <response code="200">Películas obtenidas exitosamente</response>
    /// <response code="400">Parámetros inválidos</response>
    /// <response code="401">No autorizado - Token JWT requerido</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("movies")]
    [ProducesResponseType(typeof(IEnumerable<MovieDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] int total = 50,
        [FromQuery] string order = "asc", 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obteniendo películas: total={Total}, order={Order}", total, order);

            // Validaciones de entrada
            var validationErrors = new List<string>();

            if (total <= 0 || total > 1000)
            {
                validationErrors.Add("El parámetro 'total' debe estar entre 1 y 1000");
            }

            if (!new[] { "asc", "desc" }.Contains(order.ToLowerInvariant()))
            {
                validationErrors.Add("El parámetro 'order' debe ser 'asc' o 'desc'");
            }

            if (validationErrors.Any())
            {
                _logger.LogWarning("Parámetros inválidos: {Errors}", string.Join(", ", validationErrors));
                return BadRequest(new
                {
                    title = "Parámetros inválidos",
                    status = 400,
                    detail = "Se encontraron errores en los parámetros proporcionados",
                    errors = validationErrors.Select(e => new { message = e }),
                    timestamp = DateTime.UtcNow
                });
            }

            var movies = await _movieService.GetAllAsync(total, order, "Year", cancellationToken);
            
            _logger.LogInformation("Se obtuvieron {Count} películas exitosamente", movies.Count());
            return Ok(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las películas");
            throw;
        }
    }

    /// <summary>
    /// Crea una nueva película
    /// </summary>
    /// <param name="createMovieDto">Datos de la película a crear</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Película creada</returns>
    /// <response code="201">Película creada exitosamente</response>
    /// <response code="400">Datos inválidos, película duplicada o errores de validación</response>
    /// <response code="401">No autorizado - Token JWT requerido</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("movie")]
    [ProducesResponseType(typeof(MovieDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<MovieDto>> CreateMovie(
        [FromBody] CreateMovieDto createMovieDto, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (createMovieDto == null)
            {
                _logger.LogWarning("Intento de crear película con datos nulos");
                return BadRequest(new
                {
                    title = "Datos requeridos",
                    status = 400,
                    detail = "Debe proporcionar los datos de la película a crear",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Creando nueva película: {Film}", createMovieDto.Film);

            var movie = await _movieService.AddAsync(createMovieDto, cancellationToken);
            
            _logger.LogInformation("Película creada exitosamente: {Film} con ID {Id}", movie.Film, movie.Id);
            
            return CreatedAtAction(
                nameof(GetMovie), 
                new { id = movie.Id }, 
                movie);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Error de validación al crear película: {Film}", createMovieDto?.Film);
            
            var errors = ex.Errors.Select(e => new
            {
                field = e.PropertyName,
                message = e.ErrorMessage,
                code = e.ErrorCode
            }).ToList();

            return BadRequest(new
            {
                title = "Error de validación",
                status = 400,
                detail = "Los datos proporcionados no son válidos",
                errors = errors,
                timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida al crear película: {Film}", createMovieDto?.Film);
            return BadRequest(new
            {
                title = "Operación inválida",
                status = 400,
                detail = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear película: {Film}", createMovieDto?.Film);
            throw;
        }
    }
} 