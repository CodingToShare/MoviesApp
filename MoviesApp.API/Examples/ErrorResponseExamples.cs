namespace MoviesApp.API.Examples;

/// <summary>
/// Ejemplos de respuestas de error para documentación Swagger
/// </summary>
public static class ErrorResponseExamples
{
    /// <summary>
    /// Ejemplo de error de validación completo
    /// </summary>
    public static object ValidationError => new
    {
        title = "Error de validación",
        status = 400,
        detail = "Los datos proporcionados no son válidos",
        errors = new[]
        {
            new
            {
                field = "Id",
                message = "El ID debe ser mayor a 0",
                code = "GreaterThanValidator"
            },
            new
            {
                field = "Film",
                message = "El nombre de la película es requerido",
                code = "NotEmptyValidator"
            },
            new
            {
                field = "Score",
                message = "La puntuación debe estar entre 0 y 100",
                code = "InclusiveBetweenValidator"
            }
        },
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de película no encontrada
    /// </summary>
    public static object NotFoundError => new
    {
        title = "Película no encontrada",
        status = 404,
        detail = "No se encontró una película con el ID 999",
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de parámetros inválidos
    /// </summary>
    public static object BadRequestError => new
    {
        title = "Parámetros inválidos",
        status = 400,
        detail = "Se encontraron errores en los parámetros proporcionados",
        errors = new[]
        {
            new { message = "El parámetro 'total' debe estar entre 1 y 1000" },
            new { message = "El parámetro 'order' debe ser 'asc' o 'desc'" }
        },
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de película duplicada
    /// </summary>
    public static object DuplicateMovieError => new
    {
        title = "Operación inválida",
        status = 400,
        detail = "Ya existe una película con el ID 1001",
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de error interno del servidor
    /// </summary>
    public static object InternalServerError => new
    {
        title = "Error interno del servidor",
        status = 500,
        detail = "Ha ocurrido un error interno. Por favor, contacte al administrador",
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de datos requeridos
    /// </summary>
    public static object RequiredDataError => new
    {
        title = "Datos requeridos",
        status = 400,
        detail = "Debe proporcionar los datos de la película a crear",
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de ID inválido
    /// </summary>
    public static object InvalidIdError => new
    {
        title = "ID inválido",
        status = 400,
        detail = "El ID -1 no es válido. Debe ser un número positivo mayor a 0",
        timestamp = "2024-01-15T10:30:00.000Z"
    };

    /// <summary>
    /// Ejemplo de error de modelo malformado
    /// </summary>
    public static object ModelValidationError => new
    {
        title = "Error de validación del modelo",
        status = 400,
        detail = "Se encontraron errores en los datos enviados",
        errors = new[]
        {
            new
            {
                field = "$.Id",
                message = "Se esperaba un número entero"
            },
            new
            {
                field = "$.Film",
                message = "Se requiere el campo Film"
            }
        },
        timestamp = "2024-01-15T10:30:00.000Z"
    };
} 