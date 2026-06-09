using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zorvian.Web.Filters;

/// <summary>
/// Global action filter that validates request DTOs using FluentValidation validators.
/// </summary>
public class ValidationFilter<T> : IAsyncActionFilter where T : class
{
    private readonly IValidator<T>? _validator;

    public ValidationFilter(IValidator<T>? validator = null)
    {
        _validator = validator;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_validator == null)
        {
            await next();
            return;
        }

        // Find the parameter of type T in the action arguments
        var parameter = context.ActionArguments.Values.FirstOrDefault(v => v is T) as T;
        if (parameter == null)
        {
            await next();
            return;
        }

        var validationResult = await _validator.ValidateAsync(parameter);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => char.ToLowerInvariant(g.Key[0]) + g.Key[1..],
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            context.Result = new BadRequestObjectResult(new
            {
                error = "Validación fallida",
                statusCode = 400,
                details = errors
            });
            return;
        }

        await next();
    }
}