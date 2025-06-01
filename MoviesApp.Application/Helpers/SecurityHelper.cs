using System.Text.RegularExpressions;

namespace MoviesApp.Application.Helpers;

/// <summary>
/// Helper para funciones de seguridad y sanitización
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Sanitiza la entrada específicamente para logs y previene inyección de logs
    /// </summary>
    public static string SanitizeForLogging(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remover/reemplazar caracteres de control que pueden ser usados para log injection
        var sanitized = input
            .Replace(Environment.NewLine, " ")  // Remover nuevas líneas
            .Replace("\n", " ")                 // Remover \n
            .Replace("\r", " ")                 // Remover \r
            .Replace("\t", " ")                 // Remover tabs
            .Replace("\0", "")                  // Remover null characters
            .Replace("\x1A", "")                // Remover substitute character
            .Replace("<", "")                   // Remover HTML tags
            .Replace(">", "")
            .Replace("\"", "")                  // Remover quotes
            .Replace("'", "")
            .Replace("&", "")                   // Remover HTML entities
            .Replace(";", "")                   // Remover separadores SQL
            .Replace("--", "")                  // Remover comentarios SQL
            .Replace("/*", "")                  // Remover comentarios multilinea
            .Replace("*/", "");

        // Limitar longitud para prevenir log flooding
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100] + "...";
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Valida que una cadena no contenga caracteres peligrosos
    /// </summary>
    public static bool IsInputSafe(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        var dangerousChars = new[] { "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", "--", "/*", "*/" };
        return !dangerousChars.Any(dangerousChar => 
            input.Contains(dangerousChar, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sanitiza entrada de usuario removiendo caracteres peligrosos
    /// </summary>
    public static string SanitizeUserInput(string? input, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // Remover caracteres peligrosos
        var dangerousChars = new[] { 
            "<", ">", "\"", "'", "&", "\0", "\r", "\n", ";", 
            "--", "/*", "*/", "script", "javascript", "vbscript",
            "onload", "onerror", "onclick"
        };

        var sanitized = input;
        foreach (var dangerousChar in dangerousChars)
        {
            sanitized = sanitized.Replace(dangerousChar, "", StringComparison.OrdinalIgnoreCase);
        }

        // Limitar longitud
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Valida que un email tenga formato básico correcto
    /// </summary>
    public static bool IsValidEmailFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return email.Contains('@') && 
               email.Count(c => c == '@') == 1 &&
               email.Length <= 255 &&
               !email.StartsWith('@') &&
               !email.EndsWith('@');
    }
} 