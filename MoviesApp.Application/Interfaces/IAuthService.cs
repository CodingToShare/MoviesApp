using MoviesApp.Application.DTOs.Auth;

namespace MoviesApp.Application.Interfaces;

/// <summary>
/// Interfaz para el servicio de autenticación
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<UserInfoDto?> GetUserInfoAsync(int userId, CancellationToken cancellationToken = default);
} 