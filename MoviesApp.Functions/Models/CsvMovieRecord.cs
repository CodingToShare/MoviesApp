using CsvHelper.Configuration.Attributes;

namespace MoviesApp.Functions.Models;

/// <summary>
/// Modelo para representar una película del archivo CSV
/// </summary>
public class CsvMovieRecord
{
    [Name("ID")]
    [Index(0)]
    public int Id { get; set; }

    [Name("Film")]
    [Index(1)]
    public string Film { get; set; } = string.Empty;

    [Name("Genre")]
    [Index(2)]
    public string Genre { get; set; } = string.Empty;

    [Name("Studio")]
    [Index(3)]
    public string Studio { get; set; } = string.Empty;

    [Name("Score")]
    [Index(4)]
    public int Score { get; set; }

    [Name("Year")]
    [Index(5)]
    public int Year { get; set; }

    /// <summary>
    /// Valida si el registro tiene datos válidos
    /// </summary>
    public bool IsValid()
    {
        return Id > 0 
            && !string.IsNullOrWhiteSpace(Film)
            && !string.IsNullOrWhiteSpace(Genre)
            && !string.IsNullOrWhiteSpace(Studio)
            && Score >= 0 && Score <= 100
            && Year >= 1888 && Year <= DateTime.Now.Year + 10;
    }

    /// <summary>
    /// Normaliza los datos del registro
    /// </summary>
    public void Normalize()
    {
        // Limpiar espacios en blanco
        Film = Film?.Trim() ?? string.Empty;
        Genre = Genre?.Trim() ?? string.Empty;
        Studio = Studio?.Trim() ?? string.Empty;

        // Corregir géneros comunes con errores tipográficos
        Genre = Genre switch
        {
            "Romence" => "Romance",
            "Comdy" => "Comedy",
            "romance" => "Romance",
            "comedy" => "Comedy",
            "action" => "Action",
            "drama" => "Drama",
            "Animation" => "Animation",
            _ => Genre
        };

        // Normalizar capitalización de géneros
        if (!string.IsNullOrEmpty(Genre))
        {
            Genre = char.ToUpper(Genre[0]) + Genre[1..].ToLower();
        }
    }

    /// <summary>
    /// Obtiene una descripción del registro para logging
    /// </summary>
    public string ToDescription()
    {
        return $"ID: {Id}, Film: '{Film}', Year: {Year}, Score: {Score}, Genre: '{Genre}'";
    }

    /// <summary>
    /// Obtiene errores de validación específicos
    /// </summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (Id <= 0)
            errors.Add($"ID inválido: {Id}");

        if (string.IsNullOrWhiteSpace(Film))
            errors.Add("Título de película vacío");

        if (string.IsNullOrWhiteSpace(Genre))
            errors.Add("Género vacío");

        if (string.IsNullOrWhiteSpace(Studio))
            errors.Add("Estudio vacío");

        if (Score < 0 || Score > 100)
            errors.Add($"Puntaje inválido: {Score} (debe estar entre 0-100)");

        if (Year < 1888 || Year > DateTime.Now.Year + 10)
            errors.Add($"Año inválido: {Year} (debe estar entre 1888-{DateTime.Now.Year + 10})");

        return errors;
    }
} 