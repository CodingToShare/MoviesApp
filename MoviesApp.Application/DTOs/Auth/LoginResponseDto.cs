namespace MoviesApp.Application.DTOs.Auth;

/// <summary>
/// DTO para respuesta de login exitoso
/// </summary>
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
}

/// <summary>
/// DTO con información básica del usuario
/// </summary>
public class UserInfoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
} 