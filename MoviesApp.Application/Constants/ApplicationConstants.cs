namespace MoviesApp.Application.Constants;

/// <summary>
/// Constantes globales para la capa de aplicación
/// </summary>
public static class ApplicationConstants
{
    /// <summary>
    /// Mensajes de error comunes
    /// </summary>
    public static class ErrorMessages
    {
        public const string MovieNotFound = "No se encontró la película con el ID especificado";
        public const string InvalidId = "El ID proporcionado no es válido";
        public const string DuplicateMovie = "Ya existe una película con el ID especificado";
        public const string ValidationFailed = "Los datos proporcionados no pasaron la validación";
        public const string CsvFileNotFound = "No se encontró el archivo CSV especificado";
        public const string CsvStreamInvalid = "El stream CSV proporcionado no es válido";
        public const string EmptyResultSet = "No se encontraron resultados para los criterios especificados";
    }

    /// <summary>
    /// Mensajes de éxito
    /// </summary>
    public static class SuccessMessages
    {
        public const string MovieCreated = "Película creada exitosamente";
        public const string MovieUpdated = "Película actualizada exitosamente";
        public const string MovieDeleted = "Película eliminada exitosamente";
        public const string CsvLoadCompleted = "Carga CSV completada exitosamente";
        public const string ValidationPassed = "Validación completada exitosamente";
    }

    /// <summary>
    /// Configuraciones por defecto
    /// </summary>
    public static class DefaultValues
    {
        public const int MaxPageSize = 1000;
        public const int DefaultPageSize = 50;
        public const string DefaultOrderBy = "Year";
        public const string DefaultOrder = "asc";
        public const int MinYear = 1900;
        public const int MaxYear = 2100;
        public const int MinScore = 0;
        public const int MaxScore = 100;
    }

    /// <summary>
    /// Campos de ordenamiento válidos
    /// </summary>
    public static class OrderByFields
    {
        public const string Id = "Id";
        public const string Film = "Film";
        public const string Genre = "Genre";
        public const string Studio = "Studio";
        public const string Score = "Score";
        public const string Year = "Year";
        public const string CreatedAt = "CreatedAt";
        public const string UpdatedAt = "UpdatedAt";

        public static readonly string[] ValidFields = 
        {
            Id, Film, Genre, Studio, Score, Year, CreatedAt, UpdatedAt
        };
    }

    /// <summary>
    /// Configuraciones de CSV
    /// </summary>
    public static class CsvSettings
    {
        public const int MaxBatchSize = 1000;
        public const bool ValidateDuplicatesByDefault = true;
        public const string DefaultEncoding = "UTF-8";
    }

    /// <summary>
    /// Configuraciones de logging
    /// </summary>
    public static class LoggingSettings
    {
        public const string DefaultLogFormat = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        public const int MaxLogMessageLength = 2000;
    }
} 