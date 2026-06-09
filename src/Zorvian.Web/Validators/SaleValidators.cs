using FluentValidation;

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

public class CreateCashSaleRequestValidator : BaseValidator<object>
{
    // Placeholder — wire up when DTO is available
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
