using FluentValidation;
using Microsoft.Extensions.Logging;
using MoviesApp.Application.Constants;
using MoviesApp.Application.DTOs;

namespace MoviesApp.Application.Helpers;

/// <summary>
/// Helper para manejo centralizado de excepciones y errores
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// Crea una ValidationException con formato consistente
    /// </summary>
    /// <param name="errors">Lista de errores de validación</param>
    /// <returns>ValidationException formateada</returns>
    public static ValidationException CreateValidationException(IEnumerable<string> errors)
    {
        var errorMessage = $"{ApplicationConstants.ErrorMessages.ValidationFailed}: {string.Join(", ", errors)}";
        return new ValidationException(errorMessage);
    }

    /// <summary>
    /// Crea una ValidationException desde errores de FluentValidation
    /// </summary>
    /// <param name="validationResult">Resultado de validación de FluentValidation</param>
    /// <returns>ValidationException formateada</returns>
    public static ValidationException CreateValidationException(FluentValidation.Results.ValidationResult validationResult)
    {
        var errors = validationResult.Errors.Select(e => e.ErrorMessage);
        return CreateValidationException(errors);
    }

    /// <summary>
    /// Crea una InvalidOperationException para película duplicada
    /// </summary>
    /// <param name="id">ID de la película duplicada</param>
    /// <returns>InvalidOperationException</returns>
    public static InvalidOperationException CreateDuplicateMovieException(int id)
    {
        return new InvalidOperationException($"{ApplicationConstants.ErrorMessages.DuplicateMovie}. ID: {id}");
    }

    /// <summary>
    /// Crea una ArgumentException para ID inválido
    /// </summary>
    /// <param name="id">ID inválido</param>
    /// <param name="paramName">Nombre del parámetro</param>
    /// <returns>ArgumentException</returns>
    public static ArgumentException CreateInvalidIdException(int id, string paramName = "id")
    {
        return new ArgumentException($"{ApplicationConstants.ErrorMessages.InvalidId}: {id}", paramName);
    }

    /// <summary>
    /// Crea una FileNotFoundException para archivo CSV
    /// </summary>
    /// <param name="filePath">Ruta del archivo no encontrado</param>
    /// <returns>FileNotFoundException</returns>
    public static FileNotFoundException CreateCsvFileNotFoundException(string filePath)
    {
        return new FileNotFoundException($"{ApplicationConstants.ErrorMessages.CsvFileNotFound}: {filePath}", filePath);
    }

    /// <summary>
    /// Crea un ValidationResultDto para errores de validación
    /// </summary>
    /// <param name="validationResult">Resultado de validación de FluentValidation</param>
    /// <returns>ValidationResultDto con errores</returns>
    public static ValidationResultDto CreateValidationResultDto(FluentValidation.Results.ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .Select(e => new ValidationErrorDto
            {
                Field = e.PropertyName,
                Error = e.ErrorMessage,
                ErrorCode = e.ErrorCode
            })
            .ToList();

        return ValidationResultDto.Failure(errors);
    }

    /// <summary>
    /// Registra excepción y devuelve mensaje de error seguro para el usuario
    /// </summary>
    /// <param name="logger">Logger para registrar la excepción</param>
    /// <param name="exception">Excepción a registrar</param>
    /// <param name="operation">Descripción de la operación que falló</param>
    /// <param name="context">Contexto adicional (opcional)</param>
    /// <returns>Mensaje de error seguro para mostrar al usuario</returns>
    public static string LogAndGetSafeErrorMessage(
        ILogger logger, 
        Exception exception, 
        string operation, 
        object? context = null)
    {
        // Registrar excepción completa
        if (context != null)
        {
            logger.LogError(exception, "Error en operación: {Operation}. Contexto: {@Context}", operation, context);
        }
        else
        {
            logger.LogError(exception, "Error en operación: {Operation}", operation);
        }

        // Devolver mensaje seguro según el tipo de excepción
        return exception switch
        {
            ValidationException => exception.Message,
            InvalidOperationException => exception.Message,
            ArgumentException => exception.Message,
            FileNotFoundException => ApplicationConstants.ErrorMessages.CsvFileNotFound,
            _ => "Ha ocurrido un error interno. Por favor, contacte al administrador."
        };
    }

    /// <summary>
    /// Valida que un ID sea positivo
    /// </summary>
    /// <param name="id">ID a validar</param>
    /// <param name="paramName">Nombre del parámetro</param>
    /// <exception cref="ArgumentException">Si el ID no es válido</exception>
    public static void ValidatePositiveId(int id, string paramName = "id")
    {
        if (id <= 0)
        {
            throw CreateInvalidIdException(id, paramName);
        }
    }

    /// <summary>
    /// Valida que una cadena no esté vacía
    /// </summary>
    /// <param name="value">Valor a validar</param>
    /// <param name="paramName">Nombre del parámetro</param>
    /// <exception cref="ArgumentException">Si la cadena está vacía</exception>
    public static void ValidateNotNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El valor no puede estar vacío", paramName);
        }
    }
} 