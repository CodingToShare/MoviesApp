using FluentValidation;
using System.Net;
using System.Text.Json;

namespace MoviesApp.API.Middleware;

/// <summary>
/// Middleware para manejo global de errores
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Error de validación: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Acceso no autorizado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso no encontrado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout en la operación: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Operación cancelada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Operación no soportada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no manejado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        
        // Evitar escribir si ya se empezó a escribir la respuesta
        if (response.HasStarted)
        {
            return;
        }

        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ValidationException validationEx => new ErrorResponse
            {
                Title = "Error de validación",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "Se encontraron errores de validación",
                Errors = validationEx.Errors?.Select(e => new ErrorDetail
                {
                    Field = e.PropertyName ?? "Unknown",
                    Message = e.ErrorMessage ?? "Error de validación",
                    Code = e.ErrorCode
                }).ToList() ?? new List<ErrorDetail>()
            },
            ArgumentNullException => new ErrorResponse
            {
                Title = "Argumento requerido",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "Un parámetro requerido no fue proporcionado"
            },
            ArgumentException => new ErrorResponse
            {
                Title = "Argumento inválido",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = exception.Message
            },
            InvalidOperationException => new ErrorResponse
            {
                Title = "Operación inválida",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = exception.Message
            },
            KeyNotFoundException => new ErrorResponse
            {
                Title = "Recurso no encontrado",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "El recurso solicitado no fue encontrado"
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                Title = "Acceso no autorizado",
                Status = (int)HttpStatusCode.Unauthorized,
                Detail = "No tiene permisos para acceder a este recurso"
            },
            TimeoutException => new ErrorResponse
            {
                Title = "Tiempo de espera agotado",
                Status = (int)HttpStatusCode.RequestTimeout,
                Detail = "La operación tardó demasiado tiempo en completarse"
            },
            _ => new ErrorResponse
            {
                Title = "Error interno del servidor",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Ha ocurrido un error interno. Por favor, contacte al administrador"
            }
        };

        response.StatusCode = errorResponse.Status;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        try
        {
            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await response.WriteAsync(jsonResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error serializando respuesta de error a JSON");
            // Fallback si hay error serializando
            await response.WriteAsync("{\"title\":\"Error interno\",\"status\":500}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error escribiendo respuesta al stream HTTP");
            // Fallback para errores de escritura
            await response.WriteAsync("{\"title\":\"Error interno\",\"status\":500}");
        }
    }
}

/// <summary>
/// Respuesta de error estándar
/// </summary>
public class ErrorResponse
{
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public List<ErrorDetail>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detalle de error específico
/// </summary>
public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
} 