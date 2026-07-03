using FluentValidation;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Commercial;

namespace Zorvian.Web.Validators;

public sealed class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El cliente es requerido.");
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("La sucursal es requerida.");
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");
        RuleFor(x => x.Details).NotEmpty().WithMessage("La cotización debe contener al menos un producto.");
        RuleForEach(x => x.Details).SetValidator(new QuoteDetailItemValidator());
        RuleFor(x => x.CurrencyCode).MaximumLength(3);
        When(x => x.ExchangeRateToReporting.HasValue, () =>
            RuleFor(x => x.ExchangeRateToReporting!.Value).GreaterThan(0));
    }
}

public sealed class QuoteDetailItemValidator : AbstractValidator<QuoteDetailItem>
{
    public QuoteDetailItemValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");
        RuleFor(x => x.Subtotal).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCreditRequestValidator : AbstractValidator<CreateCreditSaleRequest>
{
    public CreateCreditRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("El cliente es requerido.");
        RuleFor(x => x.EmployeeId).NotEmpty().WithMessage("El empleado es requerido.");
        RuleFor(x => x.BranchId).NotEmpty().WithMessage("La sucursal es requerida.");
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Details).NotEmpty().WithMessage("La venta debe contener al menos un producto.");
        RuleForEach(x => x.Details).SetValidator(new SaleDetailItemValidator());
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InstallmentCount).GreaterThan(0).WithMessage("Debe tener al menos una cuota.");
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCreditNoteRequestValidator : AbstractValidator<CreateCreditNoteRequest>
{
    public CreateCreditNoteRequestValidator()
    {
        RuleFor(x => x.SaleId).NotEmpty().WithMessage("La venta es requerida.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500).WithMessage("El motivo es requerido (máx. 500 caracteres).");
        RuleFor(x => x.Details).NotEmpty().WithMessage("La nota de crédito debe contener al menos un producto.");
        RuleForEach(x => x.Details).SetValidator(new CreateCreditNoteDetailItemValidator());
    }
}

public sealed class CreateCreditNoteDetailItemValidator : AbstractValidator<CreateCreditNoteDetailItem>
{
    public CreateCreditNoteDetailItemValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
    }
}

public sealed class CreateSupplierPaymentRequestValidator : AbstractValidator<CreateSupplierPaymentRequest>
{
    public CreateSupplierPaymentRequestValidator()
    {
        RuleFor(x => x.PurchaseId).NotEmpty().WithMessage("La compra es requerida.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
