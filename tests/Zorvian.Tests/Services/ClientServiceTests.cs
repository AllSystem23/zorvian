using Moq;
using FluentAssertions;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class ClientServiceTests
{
    private readonly Mock<IClientRepository> _repo = new();
    private readonly Mock<ISaleRepository> _saleRepo = new();
    private readonly Mock<ICreditRepository> _creditRepo = new();
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ClientService _sut;
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public ClientServiceTests()
    {
        _tenant.Setup(t => t.TenantId).Returns(_companyId.ToString());
        _sut = new ClientService(_repo.Object, _saleRepo.Object, _creditRepo.Object, _tenant.Object, Mock.Of<AutoMapper.IMapper>());
    }

    private Client MakeClient() => new()
    {
        Id = _clientId,
        Code = "CL-001",
        FirstName = "Juan",
        LastName = "Pérez",
        Phone = "555-0100",
        CreditLimit = 50000m,
        Status = "active",
        CompanyId = _companyId,
        BranchId = Guid.NewGuid(),
    };

    private Sale MakeSale(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        InvoiceNumber = $"INV-{Guid.NewGuid().ToString()[..4]}",
        ClientId = _clientId,
        SaleDate = DateTime.UtcNow.AddDays(-5),
        SaleType = "credit",
        Subtotal = 1000m,
        Tax = 0,
        Total = 1000m,
        PaidAmount = 200m,
        Balance = 800m,
        Status = "completed",
        CompanyId = _companyId,
        BranchId = Guid.NewGuid(),
    };

    private CreditInstallment MakeInstallment(int number, DateOnly dueDate, string status = "pending", decimal balance = 500m) => new()
    {
        Id = Guid.NewGuid(),
        CreditId = Guid.NewGuid(),
        InstallmentNumber = number,
        DueDate = dueDate,
        Amount = 500m,
        PrincipalAmount = 400m,
        InterestAmount = 100m,
        PaidAmount = status == "paid" ? 500m : 0,
        Balance = status == "paid" ? 0 : balance,
        Status = status,
        CompanyId = _companyId,
        BranchId = Guid.NewGuid(),
    };

    private Credit MakeCredit(Guid? id = null, params CreditInstallment[] installments) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CreditNumber = "CRE-001",
        ClientId = _clientId,
        FinancedAmount = 8000m,
        InterestRate = 5m,
        InstallmentCount = installments.Length > 0 ? installments.Length : 1,
        InstallmentAmount = 1000m,
        TotalAmount = 8000m,
        PaidAmount = 2000m,
        Balance = 6000m,
        InterestAmount = 0m,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
        NextDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
        Status = "active",
        CreatedAt = DateTime.UtcNow.AddDays(-60),
        CompanyId = _companyId,
        BranchId = Guid.NewGuid(),
        Installments = installments.ToList(),
    };

    [Fact]
    public async Task GetStatementAsync_ReturnsCompleteStatement()
    {
        var client = MakeClient();
        _repo.Setup(r => r.GetByIdAsync(_clientId)).ReturnsAsync(client);

        var sales = new List<Sale>
        {
            MakeSale(),
            MakeSale(),
        };
        _saleRepo.Setup(r => r.GetFilteredAsync(_clientId, null, null, null, null, Guid.Empty, 1, 50))
            .ReturnsAsync(sales);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDue = today.AddDays(-3);
        var overdueInstallments = new[]
        {
            MakeInstallment(1, pastDue, "pending", 500m),
            MakeInstallment(2, today.AddDays(5), "pending", 500m),
        };
        var credit = MakeCredit(Guid.NewGuid(), overdueInstallments);
        credit.Balance = 6000m;

        var credits = new List<Credit> { credit };
        _creditRepo.Setup(r => r.GetFilteredAsync(_clientId, null, Guid.Empty, 1, 50))
            .ReturnsAsync(credits);

        var result = await _sut.GetStatementAsync(_clientId);

        result.Should().NotBeNull();
        result!.ClientId.Should().Be(_clientId);
        result.ClientName.Should().Be("Juan Pérez");
        result.ClientCode.Should().Be("CL-001");
        result.TotalSales.Should().Be(2);
        result.ActiveCredits.Should().Be(1);
        result.TotalBalance.Should().Be(6000m);
        result.OverdueBalance.Should().Be(6000m);
        result.RecentSales.Should().HaveCount(2);
        result.ActiveCreditsList.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStatementAsync_WithNoSales_ReturnsEmptySalesList()
    {
        var client = MakeClient();
        _repo.Setup(r => r.GetByIdAsync(_clientId)).ReturnsAsync(client);

        _saleRepo.Setup(r => r.GetFilteredAsync(_clientId, null, null, null, null, Guid.Empty, 1, 50))
            .ReturnsAsync(new List<Sale>());

        var credit = MakeCredit();
        _creditRepo.Setup(r => r.GetFilteredAsync(_clientId, null, Guid.Empty, 1, 50))
            .ReturnsAsync(new List<Credit> { credit });

        var result = await _sut.GetStatementAsync(_clientId);

        result.Should().NotBeNull();
        result!.TotalSales.Should().Be(0);
        result.RecentSales.Should().BeEmpty();
        result.ActiveCredits.Should().Be(1);
        result.ActiveCreditsList.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStatementAsync_WithNoCredits_ReturnsEmptyCreditsList()
    {
        var client = MakeClient();
        _repo.Setup(r => r.GetByIdAsync(_clientId)).ReturnsAsync(client);

        var sales = new List<Sale> { MakeSale() };
        _saleRepo.Setup(r => r.GetFilteredAsync(_clientId, null, null, null, null, Guid.Empty, 1, 50))
            .ReturnsAsync(sales);

        _creditRepo.Setup(r => r.GetFilteredAsync(_clientId, null, Guid.Empty, 1, 50))
            .ReturnsAsync(new List<Credit>());

        var result = await _sut.GetStatementAsync(_clientId);

        result.Should().NotBeNull();
        result!.TotalSales.Should().Be(1);
        result.RecentSales.Should().HaveCount(1);
        result.ActiveCredits.Should().Be(0);
        result.ActiveCreditsList.Should().BeEmpty();
        result.TotalBalance.Should().Be(0);
        result.OverdueBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetStatementAsync_ClientNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync(_clientId)).ReturnsAsync((Client?)null);

        var result = await _sut.GetStatementAsync(_clientId);

        result.Should().BeNull();
    }
}
