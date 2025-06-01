using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MoviesApp.Application.DTOs.Auth;
using MoviesApp.Application.Interfaces;
using MoviesApp.Application.Helpers;
using MoviesApp.Domain.Entities;
using MoviesApp.Domain.Interfaces;
using AutoMapper;

namespace MoviesApp.Application.Services;

/// <summary>
/// Servicio de autenticación con JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly int _jwtExpirationMinutes;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IMapper mapper)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        _jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer no configurado");
        _jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar entrada
            if (loginRequest == null)
            {
                throw new ArgumentNullException(nameof(loginRequest));
            }

            // Sanitizar entrada para prevenir ataques de inyección en logs
            var sanitizedUsername = SecurityHelper.SanitizeUserInput(loginRequest.Username ?? "");

            if (string.IsNullOrWhiteSpace(sanitizedUsername))
            {
                _logger.LogWarning("Intento de login con username vacío o nulo");
                return null;
            }

            _logger.LogDebug("Intentando login para usuario: {Username}", SecurityHelper.SanitizeForLogging(sanitizedUsername));

            // Buscar usuario por username sanitizado
            var user = await _userRepository.GetByUsernameAsync(sanitizedUsername, cancellationToken);
            
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {Username}", SecurityHelper.SanitizeForLogging(sanitizedUsername));
                // Delay artificial para prevenir ataques de timing
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                return null;
            }

            // Verificar si el usuario puede hacer login
            if (!user.CanLogin())
            {
                _logger.LogWarning("Usuario inactivo intentó hacer login: {Username}", SecurityHelper.SanitizeForLogging(sanitizedUsername));
                return null;
            }

            // Verificar contraseña
            if (!VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", SecurityHelper.SanitizeForLogging(sanitizedUsername));
                // Delay artificial para prevenir ataques de timing
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                return null;
            }

            // Actualizar último login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Generar token JWT con seguridad mejorada
            var token = GenerateSecureJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            _logger.LogInformation("Login exitoso para usuario: {Username}", SecurityHelper.SanitizeForLogging(sanitizedUsername));

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "Argumento nulo en el login");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operación inválida durante el login del usuario: {Username}", SecurityHelper.SanitizeForLogging(loginRequest?.Username ?? "unknown"));
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Login cancelado para usuario: {Username}", SecurityHelper.SanitizeForLogging(loginRequest?.Username ?? "unknown"));
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout durante el login del usuario: {Username}", SecurityHelper.SanitizeForLogging(loginRequest?.Username ?? "unknown"));
            throw;
        }
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Registrando nuevo usuario: {Username}", SecurityHelper.SanitizeForLogging(registerRequest.Username));

            // Verificar si el usuario ya existe
            var existingUser = await _userRepository.ExistsAsync(registerRequest.Username, registerRequest.Email, cancellationToken);
            if (existingUser)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese username o email");
            }

            // Crear nuevo usuario
            var user = new User
            {
                Username = registerRequest.Username,
                Email = registerRequest.Email,
                PasswordHash = HashPassword(registerRequest.Password),
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                Role = "User", // Rol por defecto
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);

            // Generar token para el nuevo usuario
            var token = GenerateSecureJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            _logger.LogInformation("Usuario registrado exitosamente: {Username}", SecurityHelper.SanitizeForLogging(registerRequest.Username));

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error de validación durante el registro del usuario: {Username}", SecurityHelper.SanitizeForLogging(registerRequest?.Username ?? "unknown"));
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Operación inválida durante el registro del usuario: {Username}", SecurityHelper.SanitizeForLogging(registerRequest?.Username ?? "unknown"));
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Registro cancelado para usuario: {Username}", SecurityHelper.SanitizeForLogging(registerRequest?.Username ?? "unknown"));
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout durante el registro del usuario: {Username}", SecurityHelper.SanitizeForLogging(registerRequest?.Username ?? "unknown"));
            throw;
        }
    }

    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch (SecurityTokenException)
        {
            // Token inválido, expirado o mal formado
            return Task.FromResult(false);
        }
        catch (ArgumentException)
        {
            // Argumentos inválidos (token nulo, vacío, etc.)
            return Task.FromResult(false);
        }
        catch (FormatException)
        {
            // Token mal formateado
            return Task.FromResult(false);
        }
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            return user != null ? _mapper.Map<UserInfoDto>(user) : null;
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "ID de usuario inválido: {UserId}", userId);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario: {UserId}", userId);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Operación cancelada al obtener usuario: {UserId}", userId);
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout al obtener información del usuario: {UserId}", userId);
            throw;
        }
    }

    private string GenerateSecureJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtKey); // Cambiar a UTF8 para mejor seguridad
        
        // Agregar claims adicionales para mayor seguridad
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("jti", Guid.NewGuid().ToString()), // JWT ID único
            new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64), // Issued At
            new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Authentication Time
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtIssuer, // Agregar audience para mayor seguridad
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature),
            NotBefore = DateTime.UtcNow, // Token no válido antes de este momento
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        // Validar longitud mínima de contraseña
        if (password.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres");
        }

        // Usar BCrypt con mayor trabajo factor para mejor seguridad
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (ArgumentException)
        {
            // Argumentos inválidos (password nulo, hash inválido, etc.)
            return false;
        }
        catch (FormatException)
        {
            // Hash mal formateado
            return false;
        }
        catch (InvalidOperationException)
        {
            // Error interno de BCrypt
            return false;
        }
    }
} 