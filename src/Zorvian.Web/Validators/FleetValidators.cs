using FluentValidation;
using Zorvian.Application.DTOs.Fleet;

namespace Zorvian.Web.Validators;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Plate).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1980, 2100);
        RuleFor(x => x.Vin).MaximumLength(50);
        RuleFor(x => x.EngineNumber).MaximumLength(50);
        RuleFor(x => x.ChassisNumber).MaximumLength(50);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.VehicleTypeId).NotEmpty();
        RuleFor(x => x.FuelTypeId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CurrentKm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LoadCapacityKg).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LoadCapacityM3).GreaterThanOrEqualTo(0).When(x => x.LoadCapacityM3.HasValue);
        RuleFor(x => x.PassengerCapacity).GreaterThan(0).When(x => x.PassengerCapacity.HasValue);
        RuleFor(x => x.PurchaseValue).GreaterThanOrEqualTo(0).When(x => x.PurchaseValue.HasValue);
    }
}

public sealed class UpdateVehicleValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleValidator()
    {
        When(x => x.Code != null, () => RuleFor(x => x.Code!).MaximumLength(50));
        When(x => x.Plate != null, () => RuleFor(x => x.Plate!).MaximumLength(20));
        When(x => x.Model != null, () => RuleFor(x => x.Model!).MaximumLength(100));
        When(x => x.Year != null, () => RuleFor(x => x.Year!.Value).InclusiveBetween(1980, 2100));
        When(x => x.Vin != null, () => RuleFor(x => x.Vin!).MaximumLength(50));
        When(x => x.EngineNumber != null, () => RuleFor(x => x.EngineNumber!).MaximumLength(50));
        When(x => x.Color != null, () => RuleFor(x => x.Color!).MaximumLength(50));
        RuleFor(x => x.CurrentKm).GreaterThanOrEqualTo(0).When(x => x.CurrentKm.HasValue);
        RuleFor(x => x.LoadCapacityKg).GreaterThanOrEqualTo(0).When(x => x.LoadCapacityKg.HasValue);
        RuleFor(x => x.PurchaseValue).GreaterThanOrEqualTo(0).When(x => x.PurchaseValue.HasValue);
    }
}

public sealed class CreateDriverValidator : AbstractValidator<CreateDriverRequest>
{
    public CreateDriverValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IdDocument).NotEmpty().MaximumLength(30);
        RuleFor(x => x.BirthDate).LessThan(DateOnly.FromDateTime(DateTime.Now));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.LicenseCategoryId).NotEmpty();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.LicenseIssueDate).LessThan(x => x.LicenseExpiryDate);
        RuleFor(x => x.LicenseExpiryDate).GreaterThan(x => x.LicenseIssueDate);
        RuleFor(x => x.HireDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now));
        RuleFor(x => x.BranchId).NotEmpty();
    }
}

public sealed class UpdateDriverValidator : AbstractValidator<UpdateDriverRequest>
{
    public UpdateDriverValidator()
    {
        When(x => x.FirstName != null, () => RuleFor(x => x.FirstName!).MaximumLength(100));
        When(x => x.LastName != null, () => RuleFor(x => x.LastName!).MaximumLength(100));
        When(x => x.IdDocument != null, () => RuleFor(x => x.IdDocument!).MaximumLength(30));
        When(x => x.Phone != null, () => RuleFor(x => x.Phone!).MaximumLength(20));
        When(x => x.Email != null, () => RuleFor(x => x.Email!).EmailAddress().MaximumLength(255));
        When(x => x.Address != null, () => RuleFor(x => x.Address!).MaximumLength(500));
        When(x => x.LicenseNumber != null, () => RuleFor(x => x.LicenseNumber!).MaximumLength(30));
    }
}

public sealed class CreateVehicleBrandValidator : AbstractValidator<CreateVehicleBrandRequest>
{
    public CreateVehicleBrandValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateVehicleBrandValidator : AbstractValidator<UpdateVehicleBrandRequest>
{
    public UpdateVehicleBrandValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).MaximumLength(100));
    }
}

public sealed class CreateVehicleTypeValidator : AbstractValidator<CreateVehicleTypeRequest>
{
    public CreateVehicleTypeValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateVehicleTypeValidator : AbstractValidator<UpdateVehicleTypeRequest>
{
    public UpdateVehicleTypeValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).MaximumLength(100));
    }
}

public sealed class CreateFuelTypeValidator : AbstractValidator<CreateFuelTypeRequest>
{
    public CreateFuelTypeValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}

public sealed class UpdateFuelTypeValidator : AbstractValidator<UpdateFuelTypeRequest>
{
    public UpdateFuelTypeValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).MaximumLength(100));
    }
}

public sealed class CreateDriverLicenseCategoryValidator : AbstractValidator<CreateDriverLicenseCategoryRequest>
{
    public CreateDriverLicenseCategoryValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

public sealed class UpdateDriverLicenseCategoryValidator : AbstractValidator<UpdateDriverLicenseCategoryRequest>
{
    public UpdateDriverLicenseCategoryValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).MaximumLength(50));
    }
}

public sealed class CreateFuelRefillValidator : AbstractValidator<CreateFuelRefillRequest>
{
    public CreateFuelRefillValidator()
    {
        RuleFor(x => x.RefillDateTime).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.FuelTypeId).NotEmpty();
        RuleFor(x => x.Liters).GreaterThan(0);
        RuleFor(x => x.PricePerLiter).GreaterThan(0);
        RuleFor(x => x.TotalCost).GreaterThan(0);
        RuleFor(x => x.CurrentKm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RefillType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Observations).MaximumLength(500);
    }
}

public sealed class UpdateFuelRefillValidator : AbstractValidator<UpdateFuelRefillRequest>
{
    public UpdateFuelRefillValidator()
    {
        When(x => x.Liters != null, () => RuleFor(x => x.Liters!.Value).GreaterThan(0));
        When(x => x.PricePerLiter != null, () => RuleFor(x => x.PricePerLiter!.Value).GreaterThan(0));
        When(x => x.TotalCost != null, () => RuleFor(x => x.TotalCost!.Value).GreaterThan(0));
        When(x => x.CurrentKm != null, () => RuleFor(x => x.CurrentKm!.Value).GreaterThanOrEqualTo(0));
        When(x => x.RefillType != null, () => RuleFor(x => x.RefillType!).MaximumLength(20));
        When(x => x.PaymentMethod != null, () => RuleFor(x => x.PaymentMethod!).MaximumLength(20));
        When(x => x.Observations != null, () => RuleFor(x => x.Observations!).MaximumLength(500));
    }
}

public sealed class CreateWorkOrderValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.ReportDateTime).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CostEst).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProblemDescription).MaximumLength(2000);
        RuleFor(x => x.Diagnosis).MaximumLength(2000);
    }
}

public sealed class UpdateWorkOrderValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderValidator()
    {
        When(x => x.Number != null, () => RuleFor(x => x.Number!).MaximumLength(20));
        When(x => x.Priority != null, () => RuleFor(x => x.Priority!).MaximumLength(20));
        When(x => x.Status != null, () => RuleFor(x => x.Status!).MaximumLength(30));
        When(x => x.CostEst != null, () => RuleFor(x => x.CostEst!.Value).GreaterThanOrEqualTo(0));
        When(x => x.ProblemDescription != null, () => RuleFor(x => x.ProblemDescription!).MaximumLength(2000));
    }
}

public sealed class CreateFleetExpenseValidator : AbstractValidator<CreateFleetExpenseRequest>
{
    public CreateFleetExpenseValidator()
    {
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(30);
    }
}

public sealed class UpdateFleetExpenseValidator : AbstractValidator<UpdateFleetExpenseRequest>
{
    public UpdateFleetExpenseValidator()
    {
        When(x => x.Description != null, () => RuleFor(x => x.Description!).MaximumLength(500));
        When(x => x.Amount != null, () => RuleFor(x => x.Amount!.Value).GreaterThan(0));
        When(x => x.Currency != null, () => RuleFor(x => x.Currency!).MaximumLength(3));
        When(x => x.ExchangeRate != null, () => RuleFor(x => x.ExchangeRate!.Value).GreaterThan(0));
        When(x => x.PaymentMethod != null, () => RuleFor(x => x.PaymentMethod!).MaximumLength(30));
    }
}
