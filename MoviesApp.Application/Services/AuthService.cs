using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MoviesApp.Application.DTOs.Auth;
using MoviesApp.Application.Interfaces;
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
            // Validar input para prevenir ataques de inyección
            if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                _logger.LogWarning("Intento de login con credenciales vacías desde IP desconocida");
                return null;
            }

            // Sanitizar username para prevenir ataques
            var sanitizedUsername = SanitizeInput(loginRequest.Username);
            if (string.IsNullOrWhiteSpace(sanitizedUsername))
            {
                _logger.LogWarning("Username contiene caracteres no válidos: {Username}", SanitizeForLogging(sanitizedUsername));
                return null;
            }

            _logger.LogDebug("Intentando login para usuario: {Username}", SanitizeForLogging(sanitizedUsername));

            // Buscar usuario por username sanitizado
            var user = await _userRepository.GetByUsernameAsync(sanitizedUsername, cancellationToken);
            
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {Username}", SanitizeForLogging(sanitizedUsername));
                // Delay artificial para prevenir ataques de timing
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                return null;
            }

            // Verificar si el usuario puede hacer login
            if (!user.CanLogin())
            {
                _logger.LogWarning("Usuario inactivo intentó hacer login: {Username}", SanitizeForLogging(sanitizedUsername));
                return null;
            }

            // Verificar contraseña
            if (!VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", SanitizeForLogging(sanitizedUsername));
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

            _logger.LogInformation("Login exitoso para usuario: {Username}", SanitizeForLogging(sanitizedUsername));

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el login del usuario: {Username}", SanitizeForLogging(loginRequest.Username));
            throw;
        }
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Registrando nuevo usuario: {Username}", SanitizeForLogging(registerRequest.Username));

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

            _logger.LogInformation("Usuario registrado exitosamente: {Username}", SanitizeForLogging(registerRequest.Username));

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el registro del usuario: {Username}", SanitizeForLogging(registerRequest.Username));
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
        catch
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener información del usuario: {UserId}", userId);
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
        catch
        {
            // En caso de error en la verificación, devolver false por seguridad
            return false;
        }
    }

    /// <summary>
    /// Sanitiza la entrada para prevenir ataques de inyección
    /// </summary>
    private static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remover caracteres peligrosos
        var dangerousChars = new[] { 
            "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", 
            "--", "/*", "*/", "script", "javascript", "vbscript",
            "onload", "onerror", "onclick"
        };

        var sanitized = input;
        foreach (var dangerousChar in dangerousChars)
        {
            sanitized = sanitized.Replace(dangerousChar, "", StringComparison.OrdinalIgnoreCase);
        }

        // Limitar longitud
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitiza la entrada específicamente para logs y previene inyección de logs/forgery
    /// </summary>
    private static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remover/reemplazar caracteres de control que pueden ser usados para log injection
        var sanitized = input
            .Replace(Environment.NewLine, " ")  // Remover nuevas líneas
            .Replace("\n", " ")                 // Remover \n
            .Replace("\r", " ")                 // Remover \r
            .Replace("\t", " ")                 // Remover tabs
            .Replace("\0", "")                  // Remover null characters
            .Replace("\x1A", "")                // Remover substitute character
            .Replace("<", "")                   // Remover HTML tags
            .Replace(">", "")
            .Replace("\"", "")                  // Remover quotes
            .Replace("'", "")
            .Replace("&", "")                   // Remover HTML entities
            .Replace(";", "")                   // Remover separadores SQL
            .Replace("--", "")                  // Remover comentarios SQL
            .Replace("/*", "")                  // Remover comentarios multilinea
            .Replace("*/", "");

        // Limitar longitud para prevenir log flooding
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100] + "...";
        }

        return sanitized.Trim();
    }
} 