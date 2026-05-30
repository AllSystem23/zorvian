using FluentValidation;
using Nexora.Application.DTOs.Department;

namespace Nexora.Application.Validators;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).MaximumLength(255));
        When(x => x.Code != null, () => RuleFor(x => x.Code!).MaximumLength(50));
        When(x => x.Description != null, () => RuleFor(x => x.Description!).MaximumLength(500));
    }
}
