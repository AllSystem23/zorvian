using Moq;
using QuestPDF.Infrastructure;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Services;

public class VacationReceiptPdfServiceTests
{
    private readonly Mock<IVacationRepository> _mockVacationRepo = new();
    private readonly Mock<ICompanyRepository> _mockCompanyRepo = new();
    private readonly VacationReceiptPdfService _sut;

    static VacationReceiptPdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public VacationReceiptPdfServiceTests()
    {
        _sut = new VacationReceiptPdfService(_mockVacationRepo.Object, _mockCompanyRepo.Object);
    }

    private VacationPaymentReceipt CreateReceipt(Guid empId, Guid companyId, string countryCode = "NIC")
    {
        return new VacationPaymentReceipt
        {
            Id = Guid.NewGuid(),
            VacationRequestId = Guid.NewGuid(),
            EmployeeId = empId,
            ReceiptNumber = "RV-20261013-0001",
            StartDate = new DateOnly(2022, 4, 1),
            EndDate = new DateOnly(2022, 4, 15),
            ReturnDate = new DateOnly(2022, 4, 16),
            BusinessDays = 15,
            DailySalary = 366.67m,
            MonthlySalary = 11000m,
            VacationPay = 5500.05m,
            InssEmployeeAmount = 385.00m,
            InssEmployeeRate = 0.07m,
            CountryCode = countryCode,
            Currency = countryCode == "NIC" ? "NIO" : "HNL",
            PaymentDate = new DateOnly(2022, 4, 11),
            Observations = "Pago correspondiente a 15 días de vacaciones",
            Status = "paid",
            CompanyId = companyId,
        };
    }

    private Employee CreateEmployee(Guid id) => new()
    {
        Id = id,
        FirstName = "Wilhen",
        LastName = "Argenis Arguello Blandón",
        IdentificationNumber = "601-110393-0000Q",
        Salary = 11000m,
        HireDate = new DateOnly(2021, 4, 1),
        CountryCode = "NIC",
        SalaryType = "monthly",
    };

    private Company CreateCompany(Guid id) => new()
    {
        Id = id,
        Name = "TIENDA BRIZUELA ROMERO #2",
        LegalName = "TIENDA BRIZUELA ROMERO #2",
        TaxId = "003161197000G",
        Address = "Kukra Hill, sector #2",
        Phone = "86923278/87096603",
        Country = "NIC",
        Currency = "NIO",
    };

    [Fact]
    public async Task GenerateReceipt_ValidId_ShouldProducePdf()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var receipt = CreateReceipt(empId, companyId);
        var employee = CreateEmployee(empId);
        var company = CreateCompany(companyId);
        receipt.Employee = employee;

        _mockVacationRepo.Setup(r => r.GetReceiptByIdAsync(receipt.Id)).ReturnsAsync(receipt);
        _mockCompanyRepo.Setup(r => r.GetByIdAsync(companyId)).ReturnsAsync(company);

        // ACT
        var pdfBytes = await _sut.GenerateReceiptPdfAsync(receipt.Id);

        // ASSERT
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000, "PDF should have content");
        // PDF magic bytes
        Assert.Equal(0x25, pdfBytes[0]); // %
        Assert.Equal(0x50, pdfBytes[1]); // P
        Assert.Equal(0x44, pdfBytes[2]); // D
        Assert.Equal(0x46, pdfBytes[3]); // F
    }

    [Fact]
    public async Task GenerateReceipt_NotFound_ShouldThrow()
    {
        // ARRANGE
        var fakeId = Guid.NewGuid();
        _mockVacationRepo.Setup(r => r.GetReceiptByIdAsync(fakeId)).ReturnsAsync((VacationPaymentReceipt?)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GenerateReceiptPdfAsync(fakeId));
    }

    [Fact]
    public async Task GenerateReceipt_MultiCountry_Honduras_ShouldProducePdf()
    {
        // ARRANGE — HND receipt
        var empId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var receipt = CreateReceipt(empId, companyId, "HND");
        receipt.Employee = CreateEmployee(empId);
        var company = CreateCompany(companyId);

        _mockVacationRepo.Setup(r => r.GetReceiptByIdAsync(receipt.Id)).ReturnsAsync(receipt);
        _mockCompanyRepo.Setup(r => r.GetByIdAsync(companyId)).ReturnsAsync(company);

        // ACT
        var pdfBytes = await _sut.GenerateReceiptPdfAsync(receipt.Id);

        // ASSERT
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
    }

    [Fact]
    public async Task GenerateReceipt_ZeroNetPay_ShouldStillProducePdf()
    {
        // ARRANGE — edge case: net pay = 0 (fully deducted)
        var empId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var receipt = CreateReceipt(empId, companyId);
        receipt.VacationPay = 100m;
        receipt.InssEmployeeAmount = 100m; // Net = 0
        receipt.Employee = CreateEmployee(empId);
        var company = CreateCompany(companyId);

        _mockVacationRepo.Setup(r => r.GetReceiptByIdAsync(receipt.Id)).ReturnsAsync(receipt);
        _mockCompanyRepo.Setup(r => r.GetByIdAsync(companyId)).ReturnsAsync(company);

        // ACT
        var pdfBytes = await _sut.GenerateReceiptPdfAsync(receipt.Id);

        // ASSERT
        Assert.NotNull(pdfBytes);
    }

    [Fact]
    public async Task GenerateReceipt_WithNullFields_ShouldStillProducePdf()
    {
        // ARRANGE — edge case: company with null optional fields
        var empId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var receipt = CreateReceipt(empId, companyId);
        receipt.Observations = null;
        receipt.PaidBy = null;
        receipt.Employee = CreateEmployee(empId);
        receipt.Employee.IdentificationNumber = null;
        var company = new Company
        {
            Id = companyId,
            Name = "Test Co",
            LegalName = string.Empty,
            TaxId = null,
            Address = null,
            Phone = null,
        };

        _mockVacationRepo.Setup(r => r.GetReceiptByIdAsync(receipt.Id)).ReturnsAsync(receipt);
        _mockCompanyRepo.Setup(r => r.GetByIdAsync(companyId)).ReturnsAsync(company);

        // ACT
        var pdfBytes = await _sut.GenerateReceiptPdfAsync(receipt.Id);

        // ASSERT
        Assert.NotNull(pdfBytes);
    }
}
