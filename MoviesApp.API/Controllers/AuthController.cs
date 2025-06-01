using Microsoft.AspNetCore.Mvc;
using MoviesApp.Application.DTOs.Auth;
using MoviesApp.Application.Interfaces;
using MoviesApp.Application.Helpers;
using FluentValidation;
using System.Net;
using System.Linq;

namespace MoviesApp.API.Controllers;

/// <summary>
/// Controlador de autenticación
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly IValidator<RegisterRequestDto> _registerValidator;

    public AuthController(
        IAuthService authService, 
        ILogger<AuthController> logger,
        IValidator<LoginRequestDto> loginValidator,
        IValidator<RegisterRequestDto> registerValidator)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loginValidator = loginValidator ?? throw new ArgumentNullException(nameof(loginValidator));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
    }

    /// <summary>
    /// Inicia sesión con credenciales de usuario
    /// </summary>
    /// <param name="loginRequest">Credenciales de login</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Token JWT y información del usuario</returns>
    /// <response code="200">Login exitoso, retorna token JWT</response>
    /// <response code="400">Datos de login inválidos</response>
    /// <response code="401">Credenciales incorrectas</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto loginRequest, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Asegurar que siempre tenemos un objeto válido para prevenir bypass controlado por usuario
            // Si loginRequest es null, usar un objeto vacío que fallará la validación
            var safeLoginRequest = loginRequest ?? new LoginRequestDto { Username = "", Password = "" };
            
            // Siempre ejecutar la lógica de autenticación - no permitir bypass controlado por usuario
            // La validación manejará inputs nulos/inválidos de forma segura
            var validationResult = await _loginValidator.ValidateAsync(safeLoginRequest, cancellationToken);
            if (!validationResult.IsValid)
            {
                // Log sin exponer información sensible
                _logger.LogWarning("Intento de login con datos inválidos");
                
                return BadRequest(new
                {
                    title = "Datos inválidos",
                    status = 400,
                    detail = "Los datos proporcionados no son válidos",
                    errors = validationResult.Errors.Select(e => e.ErrorMessage),
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Procesando login para usuario: {Username}", 
                SecurityHelper.SanitizeForLogging(safeLoginRequest.Username));

            // Siempre llamar al servicio de autenticación con datos validados
            // Esto asegura que la autenticación siempre se ejecute
            var result = await _authService.LoginAsync(safeLoginRequest, cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Login fallido para usuario: {Username}", 
                    SecurityHelper.SanitizeForLogging(safeLoginRequest.Username));
                
                return Unauthorized(new
                {
                    title = "Credenciales incorrectas",
                    status = 401,
                    detail = "El usuario o contraseña son incorrectos",
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Login exitoso para usuario: {Username}", 
                SecurityHelper.SanitizeForLogging(safeLoginRequest.Username));
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el login del usuario: {Username}", 
                SecurityHelper.SanitizeForLogging(loginRequest?.Username));
            throw;
        }
    }

    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    /// <param name="registerRequest">Datos de registro</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Token JWT y información del usuario registrado</returns>
    /// <response code="201">Usuario registrado exitosamente</response>
    /// <response code="400">Datos de registro inválidos o usuario ya existe</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponseDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Register(
        [FromBody] RegisterRequestDto registerRequest, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Asegurar que siempre tenemos un objeto válido para prevenir bypass controlado por usuario
            // Si registerRequest es null, usar un objeto vacío que fallará la validación
            var safeRegisterRequest = registerRequest ?? new RegisterRequestDto 
            { 
                Username = "", 
                Password = "", 
                ConfirmPassword = "", 
                Email = "" 
            };
            
            // Siempre ejecutar la lógica de registro - no permitir bypass controlado por usuario
            // La validación manejará inputs nulos/inválidos de forma segura
            var validationResult = await _registerValidator.ValidateAsync(safeRegisterRequest, cancellationToken);
            if (!validationResult.IsValid)
            {
                // Log sin exponer información sensible
                _logger.LogWarning("Intento de registro con datos inválidos");
                
                return BadRequest(new
                {
                    title = "Datos inválidos",
                    status = 400,
                    detail = "Los datos proporcionados no son válidos",
                    errors = validationResult.Errors.Select(e => e.ErrorMessage),
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogDebug("Procesando registro para usuario: {Username}", 
                SecurityHelper.SanitizeForLogging(safeRegisterRequest.Username));

            // Siempre llamar al servicio de registro con datos validados
            var result = await _authService.RegisterAsync(safeRegisterRequest, cancellationToken);

            _logger.LogInformation("Usuario registrado exitosamente: {Username}", 
                SecurityHelper.SanitizeForLogging(safeRegisterRequest.Username));
            
            return CreatedAtAction(
                nameof(GetUserInfo), 
                new { id = result.User.Id }, 
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error de validación en registro: {Username}", 
                SecurityHelper.SanitizeForLogging(registerRequest?.Username));
            
            return BadRequest(new
            {
                title = "Error de registro",
                status = 400,
                detail = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el registro del usuario: {Username}", 
                SecurityHelper.SanitizeForLogging(registerRequest?.Username));
            throw;
        }
    }

    /// <summary>
    /// Obtiene información del usuario autenticado
    /// </summary>
    /// <param name="id">ID del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Información del usuario</returns>
    /// <response code="200">Información del usuario obtenida exitosamente</response>
    /// <response code="404">Usuario no encontrado</response>
    /// <response code="500">Error interno del servidor</response>
    [HttpGet("user/{id}")]
    [ProducesResponseType(typeof(UserInfoDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo(
        int id, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obteniendo información del usuario: {UserId}", id);

            var userInfo = await _authService.GetUserInfoAsync(id, cancellationToken);

            if (userInfo == null)
            {
                _logger.LogWarning("Usuario no encontrado: {UserId}", id);
                return NotFound(new
                {
                    title = "Usuario no encontrado",
                    status = 404,
                    detail = $"No se encontró un usuario con el ID {id}",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario: {UserId}", id);
            throw;
        }
    }
} 