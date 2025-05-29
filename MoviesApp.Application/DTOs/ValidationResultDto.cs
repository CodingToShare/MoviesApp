namespace MoviesApp.Application.DTOs;

/// <summary>
/// DTO que representa el resultado de una validación
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Indica si la validación fue exitosa
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Lista de errores de validación
    /// </summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Mensaje general de la validación
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Constructor para validación exitosa
    /// </summary>
    public static ValidationResultDto Success()
    {
        return new ValidationResultDto
        {
            IsValid = true,
            Message = "Validación exitosa"
        };
    }

    /// <summary>
    /// Constructor para validación fallida
    /// </summary>
    public static ValidationResultDto Failure(List<ValidationErrorDto> errors)
    {
        return new ValidationResultDto
        {
            IsValid = false,
            Errors = errors,
            Message = "Errores de validación encontrados"
        };
    }

    /// <summary>
    /// Constructor para validación fallida con un solo error
    /// </summary>
    public static ValidationResultDto Failure(string field, string error)
    {
        return new ValidationResultDto
        {
            IsValid = false,
            Errors = new List<ValidationErrorDto>
            {
                new ValidationErrorDto { Field = field, Error = error }
            },
            Message = "Error de validación encontrado"
        };
    }
}

/// <summary>
/// DTO que representa un error de validación específico
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Campo que tiene el error
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de error
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Código de error (opcional)
    /// </summary>
    public string? ErrorCode { get; set; }
} 