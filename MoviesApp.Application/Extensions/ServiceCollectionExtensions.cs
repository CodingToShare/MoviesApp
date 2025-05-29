using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.Interfaces;
using MoviesApp.Application.Mappings;
using MoviesApp.Application.Services;
using MoviesApp.Application.Validators;

namespace MoviesApp.Application.Extensions;

/// <summary>
/// Extensiones adicionales para configurar servicios de la aplicación
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura AutoMapper con validación de configuración
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <returns>Colección de servicios configurada</returns>
    public static IServiceCollection AddAutoMapperWithValidation(this IServiceCollection services)
    {
        services.AddAutoMapper(config =>
        {
            config.AddProfile<MovieMappingProfile>();
        });

        return services;
    }

    /// <summary>
    /// Configura FluentValidation con configuraciones personalizadas
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <returns>Colección de servicios configurada</returns>
    public static IServiceCollection AddCustomFluentValidation(this IServiceCollection services)
    {
        // Registrar validadores específicos
        services.AddScoped<IValidator<CreateMovieDto>, CreateMovieDtoValidator>();
        services.AddScoped<IValidator<UpdateMovieDto>, UpdateMovieDtoValidator>();

        // Configuraciones globales
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        return services;
    }

    /// <summary>
    /// Registra todos los servicios de aplicación con configuración específica
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <returns>Colección de servicios configurada</returns>
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // Servicios principales
        services.AddScoped<IMovieService, MovieService>();

        // AutoMapper
        services.AddAutoMapperWithValidation();

        // FluentValidation
        services.AddCustomFluentValidation();

        return services;
    }
} 