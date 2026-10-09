using System.Text.RegularExpressions;
using FluentValidation;
using Zorvian.Application.DTOs.Employee;

namespace Zorvian.Application.Validators;

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Position).MaximumLength(255);
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);
        RuleFor(x => x.SalaryType).MaximumLength(20);
        RuleFor(x => x.IdentificationType).MaximumLength(30);
        RuleFor(x => x.IdentificationNumber).MaximumLength(30);
        RuleFor(x => x.IdentificationNumber)
            .Must((request, number) => EmployeeValidationRules.IsValidIdentification(request.IdentificationType, number))
            .When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber))
            .WithMessage("Número de documento de identidad inválido para el tipo seleccionado.");
    }
}

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeValidator()
    {
        When(x => x.Email != null, () =>
            RuleFor(x => x.Email!).EmailAddress().MaximumLength(255));
        When(x => x.FirstName != null, () =>
            RuleFor(x => x.FirstName!).MaximumLength(100));
        When(x => x.LastName != null, () =>
            RuleFor(x => x.LastName!).MaximumLength(100));
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);
        RuleFor(x => x.IdentificationType).MaximumLength(30);
        RuleFor(x => x.IdentificationNumber).MaximumLength(30);
        RuleFor(x => x.IdentificationNumber)
            .Must((request, number) => EmployeeValidationRules.IsValidIdentification(request.IdentificationType, number))
            .When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber))
            .WithMessage("Número de documento de identidad inválido para el tipo seleccionado.");
    }
}

/// <summary>
/// Reglas de formato para documentos de identidad de Centroamérica.
/// Los patrones deben mantenerse en sincronía con
/// `frontend/lib/features/employees/identification_documents.dart`.
/// Tipos no reconocidos se aceptan sin validación de formato (solo longitud),
/// para no romper integraciones que usen otros códigos.
/// </summary>
public static class EmployeeValidationRules
{
    private static readonly Dictionary<string, string> IdentificationPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cedula_ni"] = @"^\d{3}-\d{6}-\d{4}[A-Za-z]$",
        ["nit_ni"] = @"^\d{4}-\d{6}-\d{3}-\d$",
        ["cedula_cr"] = @"^\d{9}$",
        ["cedula_pa"] = @"^\d{7,8}[A-Za-z]?$",
        ["dui_sv"] = @"^\d{8}-?\d$",
        ["nit_sv"] = @"^\d{14}$",
        ["dpi_gt"] = @"^\d{13}$",
        ["nit_gt"] = @"^\d{4}-?\d{6}-?\d{3}$",
        ["dui_hn"] = @"^\d{13}$",
        ["rtn_hn"] = @"^\d{14}$",
        ["pasaporte"] = @"^[A-Za-z0-9-]{6,20}$",
    };

    public static bool IsValidIdentification(string? type, string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return true; // opcional en backend; el frontend lo exige
        if (!IdentificationPatterns.TryGetValue(type ?? string.Empty, out var pattern))
            return true; // tipo desconocido → no validar formato
        return Regex.IsMatch(number.Trim(), pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    }
}
