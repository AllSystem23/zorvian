using FluentValidation;
using Zorvian.Application.DTOs.Commercial;

namespace Zorvian.Web.Validators;

/// <summary>
/// Base validator with common rules for all request DTOs.
/// </summary>
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator()
    {
    }
}

// ─── Sale Validators ───

public class SaleDetailItemValidator : AbstractValidator<SaleDetailItem>
{
    public SaleDetailItemValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0);
    }
}

public class CreateCashSaleRequestValidator : BaseValidator<CreateCashSaleRequest>
{
    public CreateCashSaleRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Details).NotEmpty().WithMessage("La venta debe contener al menos un producto.");
        RuleForEach(x => x.Details).SetValidator(new SaleDetailItemValidator());
        RuleFor(x => x.Payment).NotNull();
        RuleFor(x => x.Payment.Amount).GreaterThan(0);
    }
}

public class CreateCreditSaleRequestValidator : BaseValidator<CreateCreditSaleRequest>
{
    public CreateCreditSaleRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Details).NotEmpty().WithMessage("La venta debe contener al menos un producto.");
        RuleForEach(x => x.Details).SetValidator(new SaleDetailItemValidator());
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InstallmentCount).GreaterThan(0);
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0);
    }
}

// ─── Employee Validators ───

public class EmployeeCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}

// ─── Client Validators ───

public class ClientCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}

// ─── Product Validators ───

public class ProductCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}

// ─── Purchase Validators ───

public class PurchaseCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}

// ─── Credit Validators ───

public class CreditCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}

// ─── Company Validators ───

public class CompanyCreateValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
}
