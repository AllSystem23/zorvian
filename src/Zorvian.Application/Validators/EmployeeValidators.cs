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
    }
}
