using MoviesApp.Application;
using MoviesApp.Infrastructure;
using MoviesApp.Infrastructure.Data;
using MoviesApp.API.Middleware;
using MoviesApp.API.Filters;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging mejorado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

WebApplication? app = null;

try
{
    // ===============================
    // CONFIGURACIÓN SEGURA DE SECRETOS
    // ===============================
    
    // Cargar secretos desde variables de entorno de forma segura
    var connectionString = Environment.GetEnvironmentVariable("MOVIESAPP_CONNECTION_STRING") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string no configurada. Use MOVIESAPP_CONNECTION_STRING environment variable.");

    var jwtKey = Environment.GetEnvironmentVariable("MOVIESAPP_JWT_KEY") 
        ?? builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT Key no configurada. Use MOVIESAPP_JWT_KEY environment variable.");

    var jwtIssuer = Environment.GetEnvironmentVariable("MOVIESAPP_JWT_ISSUER") 
        ?? builder.Configuration["Jwt:Issuer"]
        ?? "MoviesApp.API";

    // Validar que la clave JWT tenga suficiente longitud para seguridad
    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("JWT Key debe tener al menos 32 caracteres para seguridad.");
    }

    // Configurar el connection string de forma segura
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    builder.Configuration["Jwt:Key"] = jwtKey;
    builder.Configuration["Jwt:Issuer"] = jwtIssuer;

    // Agregar servicios de capas de aplicación e infraestructura
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Configurar autenticación JWT con validación mejorada
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(5), // Reducir tolerancia de tiempo
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = "No autorizado",
                        status = 401,
                        detail = "Token JWT requerido para acceder a este recurso",
                        timestamp = DateTime.UtcNow
                    });

                    return context.Response.WriteAsync(result);
                }
            };
        });

    builder.Services.AddAuthorization();

    // Configurar controladores con filtros de validación
    builder.Services.AddControllers(options =>
    {
        // Agregar filtro de validación global
        options.Filters.Add<ValidateModelFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Deshabilitar la validación automática para usar nuestro filtro personalizado
        options.SuppressModelStateInvalidFilter = true;
    });

    // Configurar FluentValidation
    builder.Services.AddFluentValidationAutoValidation(config =>
    {
        // Deshabilitar validación automática de DataAnnotations para usar solo FluentValidation
        config.DisableDataAnnotationsValidation = true;
    });

    // Configurar manejo de errores personalizado
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new
                {
                    field = x.Key,
                    message = string.IsNullOrEmpty(e.ErrorMessage) ? "Error de validación" : e.ErrorMessage
                }))
                .ToList();

            var response = new
            {
                title = "Error de validación",
                status = 400,
                detail = "Se encontraron errores en los datos proporcionados",
                errors = errors,
                timestamp = DateTime.UtcNow
            };

            return new BadRequestObjectResult(response);
        };
    });

    // Configurar Swagger/OpenAPI con autenticación JWT
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = builder.Configuration["Api:Title"] ?? "MoviesApp API",
            Version = builder.Configuration["Api:Version"] ?? "v1",
            Description = builder.Configuration["Api:Description"] ?? "API para gestión de películas con autenticación JWT"
        });

        // Configurar autenticación JWT en Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Documentar códigos de respuesta comunes
        c.MapType<ProblemDetails>(() => new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
            {
                ["title"] = new() { Type = "string" },
                ["status"] = new() { Type = "integer" },
                ["detail"] = new() { Type = "string" },
                ["errors"] = new() { Type = "array" }
            }
        });
    });

    app = builder.Build();

    // ✅ Ejecutar migraciones automáticamente al iniciar la aplicación
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<MoviesDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("🔄 Verificando migraciones pendientes...");
            
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("📝 Se encontraron {Count} migraciones pendientes: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                    
                logger.LogInformation("⚙️ Ejecutando migraciones automáticamente...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migraciones ejecutadas exitosamente");
            }
            else
            {
                logger.LogInformation("✅ Base de datos actualizada - No hay migraciones pendientes");
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Error al ejecutar migraciones automáticamente");
            throw; // Re-lanzar la excepción para que la aplicación no inicie con problemas de BD
        }
    }

    // Inicializar base de datos con datos de prueba
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            await SeedData.InitializeAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error al inicializar datos de prueba");
        }
    }

    // Configurar el pipeline HTTP
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoviesApp API v1");
            c.DisplayRequestDuration();
            c.EnableTryItOutByDefault();
        });
    }

    // Usar middleware de manejo de errores personalizado en todos los entornos
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseHttpsRedirection();
    
    // Configurar autenticación y autorización
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // Log de errores críticos durante el startup
    var logger = app?.Services?.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Error crítico durante el inicio de la aplicación");
    throw;
}
