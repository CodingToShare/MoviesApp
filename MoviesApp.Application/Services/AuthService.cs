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
            _logger.LogDebug("Intentando login para usuario: {Username}", loginRequest.Username);

            // Buscar usuario por username
            var user = await _userRepository.GetByUsernameAsync(loginRequest.Username, cancellationToken);
            
            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado: {Username}", loginRequest.Username);
                return null;
            }

            // Verificar si el usuario puede hacer login
            if (!user.CanLogin())
            {
                _logger.LogWarning("Usuario inactivo intentó hacer login: {Username}", loginRequest.Username);
                return null;
            }

            // Verificar contraseña
            if (!VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", loginRequest.Username);
                return null;
            }

            // Actualizar último login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Generar token JWT
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            _logger.LogInformation("Login exitoso para usuario: {Username}", loginRequest.Username);

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el login del usuario: {Username}", loginRequest.Username);
            throw;
        }
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Registrando nuevo usuario: {Username}", registerRequest.Username);

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
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            _logger.LogInformation("Usuario registrado exitosamente: {Username}", registerRequest.Username);

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = _mapper.Map<UserInfoDto>(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el registro del usuario: {Username}", registerRequest.Username);
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

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes),
            Issuer = _jwtIssuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        // Usar BCrypt para hash de contraseñas
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
} 