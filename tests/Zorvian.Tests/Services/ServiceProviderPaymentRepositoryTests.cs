using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de filtro por sucursal del ServiceProviderPaymentRepository. El pago NO
/// tiene BranchId propio: se filtra por la sucursal DEL PRESTADOR (Employee.BranchId).
/// Guid.Empty/null = todas las sucursales; un GUID concreto filtra por igualdad.
/// Ojo con el Include(Employee): nav requerida = INNER JOIN en el proveedor
/// in-memory → hay que sembrar los empleados.
/// </summary>
public sealed class ServiceProviderPaymentRepositoryTests : IDisposable
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly ServiceProviderPaymentRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _company = Guid.NewGuid();
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();

    public ServiceProviderPaymentRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new ServiceProviderPaymentRepository(_db);

        SeedBranch(_branchA, "Sucursal Central");
        SeedBranch(_branchB, "Sucursal Norte");
        SeedProvider("PP-A", _branchA, "Agente Central");
        SeedProvider("PP-B", _branchB, "Agente Norte");
        SeedProvider("PP-S", null, "Agente sin Sucursal");
    }

    public void Dispose() => _db.Dispose();

    private void SeedBranch(Guid id, string name)
    {
        _db.Branches.Add(new Branch
        {
            Id = id,
            TenantId = _tenantId,
            CompanyId = _company,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    private void SeedProvider(string paymentNumber, Guid? branchId, string firstName)
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            BranchId = branchId,
            FirstName = firstName,
            LastName = "Prueba",
            EmployeeCode = paymentNumber.Replace("PP-", "PS-"),
            CollaboratorType = "service_provider",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        };
        _db.Employees.Add(employee);
        _db.ServiceProviderPayments.Add(new ServiceProviderPayment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = _company,
            PaymentNumber = paymentNumber,
            EmployeeId = employee.Id,
            Employee = employee,
            PeriodYear = 2026,
            PeriodMonth = 10,
            PeriodHalf = 1,
            PaymentDate = new DateOnly(2026, 10, 15),
            Status = "paid",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test",
        });
        _db.SaveChanges();
    }

    private static Task<(List<ServiceProviderPayment> Items, int Total)> All(
        IServiceProviderPaymentRepository repo, Guid? branchId) =>
        repo.GetFilteredAsync(null, null, null, null, null, null, branchId, 1, 50);

    [Fact]
    public async Task GetFiltered_EmptyBranch_ReturnsAllPayments()
    {
        var (items, total) = await All(_sut, Guid.Empty);

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetFiltered_NullBranch_ReturnsAllPayments()
    {
        var (items, total) = await All(_sut, null);

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetFiltered_ConcreteBranch_ReturnsOnlyProvidersOfThatBranch()
    {
        var (items, total) = await All(_sut, _branchA);

        Assert.Equal(1, total);
        Assert.Equal("PP-A", items.Single().PaymentNumber);
    }

    [Fact]
    public async Task GetFiltered_SecondBranch_ReturnsOnlyItsProviders()
    {
        var (items, total) = await All(_sut, _branchB);

        Assert.Equal(1, total);
        Assert.Equal("PP-B", items.Single().PaymentNumber);
    }

    [Fact]
    public async Task GetFiltered_ProviderWithoutBranch_OnlyVisibleInAllBranches()
    {
        // El prestador sin sucursal asignada NO aparece al filtrar por una sucursal
        // concreta, pero sí en "todas" (ya cubierto por los tests Empty/Null).
        var (items, _) = await All(_sut, _branchA);

        Assert.DoesNotContain(items, p => p.PaymentNumber == "PP-S");
    }

    [Fact]
    public async Task GetFiltered_UnknownBranch_ReturnsEmpty()
    {
        var (items, total) = await All(_sut, Guid.NewGuid());

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetById_LoadsEmployeeBranch_ForDetailAndVoucher()
    {
        var payment = await _db.ServiceProviderPayments
            .Where(p => p.PaymentNumber == "PP-A")
            .Select(p => p.Id)
            .FirstAsync();

        var loaded = await _sut.GetByIdAsync(payment);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.Employee);
        Assert.NotNull(loaded.Employee.Branch); // el nombre de sucursal se imprime en el comprobante PDF
        Assert.Equal("Sucursal Central", loaded.Employee.Branch!.Name);
    }
}
