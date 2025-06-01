using FluentValidation;
using System.Net;
using System.Text.Json;
using System.Security;
using System.Runtime.InteropServices;

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
        catch (IndexOutOfRangeException ex)
        {
            _logger.LogError(ex, "Índice fuera de rango: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (InvalidCastException ex)
        {
            _logger.LogError(ex, "Conversión inválida: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Error de formato: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (OverflowException ex)
        {
            _logger.LogError(ex, "Desbordamiento numérico: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (DivideByZeroException ex)
        {
            _logger.LogError(ex, "División por cero: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogCritical(ex, "Memoria insuficiente: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (StackOverflowException ex)
        {
            _logger.LogCritical(ex, "Desbordamiento de pila: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Archivo no encontrado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directorio no encontrado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error de entrada/salida: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error de JSON: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de solicitud HTTP: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (SecurityException ex)
        {
            _logger.LogError(ex, "Error de seguridad: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogError(ex, "Funcionalidad no implementada: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (SystemException ex)
        {
            _logger.LogError(ex, "Error del sistema: {Message}", ex.Message);
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
            TaskCanceledException => new ErrorResponse
            {
                Title = "Operación cancelada",
                Status = (int)HttpStatusCode.RequestTimeout,
                Detail = "La operación fue cancelada"
            },
            NotSupportedException => new ErrorResponse
            {
                Title = "Operación no soportada",
                Status = (int)HttpStatusCode.NotImplemented,
                Detail = "La operación solicitada no es soportada"
            },
            IndexOutOfRangeException => new ErrorResponse
            {
                Title = "Error interno",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Se produjo un error interno del servidor"
            },
            InvalidCastException => new ErrorResponse
            {
                Title = "Error de conversión",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "Error en la conversión de datos"
            },
            FormatException => new ErrorResponse
            {
                Title = "Formato inválido",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "El formato de los datos no es válido"
            },
            OverflowException => new ErrorResponse
            {
                Title = "Error de desbordamiento",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "El valor proporcionado es demasiado grande"
            },
            DivideByZeroException => new ErrorResponse
            {
                Title = "Error matemático",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "División por cero no permitida"
            },
            OutOfMemoryException => new ErrorResponse
            {
                Title = "Error de memoria",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Memoria insuficiente para procesar la solicitud"
            },
            StackOverflowException => new ErrorResponse
            {
                Title = "Error de desbordamiento",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Se produjo un desbordamiento de pila"
            },
            FileNotFoundException => new ErrorResponse
            {
                Title = "Archivo no encontrado",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "El archivo solicitado no fue encontrado"
            },
            DirectoryNotFoundException => new ErrorResponse
            {
                Title = "Directorio no encontrado",
                Status = (int)HttpStatusCode.NotFound,
                Detail = "El directorio solicitado no fue encontrado"
            },
            IOException => new ErrorResponse
            {
                Title = "Error de entrada/salida",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Error al acceder al sistema de archivos"
            },
            JsonException => new ErrorResponse
            {
                Title = "Error de JSON",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "Error en el formato JSON"
            },
            HttpRequestException => new ErrorResponse
            {
                Title = "Error de solicitud HTTP",
                Status = (int)HttpStatusCode.BadGateway,
                Detail = "Error al procesar solicitud HTTP externa"
            },
            SecurityException => new ErrorResponse
            {
                Title = "Error de seguridad",
                Status = (int)HttpStatusCode.Forbidden,
                Detail = "Acceso denegado por política de seguridad"
            },
            NotImplementedException => new ErrorResponse
            {
                Title = "Funcionalidad no implementada",
                Status = (int)HttpStatusCode.NotImplemented,
                Detail = "La funcionalidad solicitada aún no está implementada"
            },
            SystemException => new ErrorResponse
            {
                Title = "Error del sistema",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "Se produjo un error interno del sistema"
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