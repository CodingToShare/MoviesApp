using System.ComponentModel.DataAnnotations;

namespace MoviesApp.Domain.Entities;

/// <summary>
/// Entidad User para autenticación
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Método para verificar si el usuario está activo y puede autenticarse
    /// </summary>
    public bool CanLogin() => IsActive;

    /// <summary>
    /// Actualiza la fecha del último login
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si el usuario tiene un rol específico
    /// </summary>
    public bool HasRole(string role)
    {
        return string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica si el usuario es administrador
    /// </summary>
    public bool IsAdmin() => HasRole("Admin");
} 