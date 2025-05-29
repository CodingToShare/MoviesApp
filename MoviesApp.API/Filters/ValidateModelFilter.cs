using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MoviesApp.API.Filters;

/// <summary>
/// Filtro para validación automática del modelo
/// </summary>
public class ValidateModelFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new
                {
                    Field = x.Key,
                    Message = string.IsNullOrEmpty(e.ErrorMessage) ? "Error de validación" : e.ErrorMessage
                }))
                .ToList();

            var errorResponse = new
            {
                title = "Error de validación del modelo",
                status = 400,
                detail = "Se encontraron errores en los datos enviados",
                errors = errors.Select(e => new
                {
                    field = e.Field,
                    message = e.Message
                }),
                timestamp = DateTime.UtcNow
            };

            context.Result = new BadRequestObjectResult(errorResponse);
            return;
        }

        base.OnActionExecuting(context);
    }
} 