using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zorvian.Web.Filters;

/// <summary>
/// Global action filter that validates request DTOs using FluentValidation validators.
/// Resolves the correct validator dynamically from DI based on the parameter type.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Type> ValidatorTypeCache = new();

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg is null) continue;

            var argType = arg.GetType();
            if (argType.IsPrimitive || argType == typeof(string)) continue;

            var validatorType = ValidatorTypeCache.GetOrAdd(argType, t =>
                typeof(IValidator<>).MakeGenericType(t));

            var validator = _serviceProvider.GetService(validatorType) as IValidator;
            if (validator is null) continue;

            var validationResult = await validator.ValidateAsync(new ValidationContext<object>(arg));

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
        }

        await next();
    }
}