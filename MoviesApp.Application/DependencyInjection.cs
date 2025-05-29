using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MoviesApp.Application.DTOs;
using MoviesApp.Application.DTOs.Auth;
using MoviesApp.Application.Interfaces;
using MoviesApp.Application.Mappings;
using MoviesApp.Application.Services;
using MoviesApp.Application.Validators;
using System.Reflection;
using AutoMapper;

namespace MoviesApp.Application;

/// <summary>
/// Clase de extensión para configurar la inyección de dependencias de la capa de aplicación
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los servicios de la capa de aplicación al contenedor de dependencias
    /// </summary>
    /// <param name="services">Colección de servicios</param>
    /// <returns>Colección de servicios configurada</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Registrar AutoMapper con los perfiles de mapeo
        services.AddAutoMapper(typeof(MovieMappingProfile));

        // Registrar validadores para movies
        services.AddScoped<IValidator<CreateMovieDto>, CreateMovieDtoValidator>();

        // Registrar validadores para autenticación
        services.AddScoped<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
        services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();

        // Registrar servicios de aplicación
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
} 