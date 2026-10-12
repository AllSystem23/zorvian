using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Matriz de visibilidad por sucursal del QuoteRepository.
/// Regresión del bug: el repo aplicaba .Where(q => q.BranchId == branchId) sin
/// guardia, así que con Guid.Empty ("todas") solo devolvía cotizaciones sin sucursal.
/// </summary>
public sealed class QuoteRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly QuoteRepository _sut;
    private readonly string _tenantId;
    private readonly Guid _branchA = Guid.NewGuid();
    private readonly Guid _branchB = Guid.NewGuid();

    public QuoteRepositoryTests()
    {
        _tenantId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantId);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new QuoteRepository(_db);

        SeedQuote(_branchA, "COT-A");
        SeedQuote(_branchA, "COT-A2");
        SeedQuote(_branchB, "COT-B");
    }

    private void SeedQuote(Guid branchId, string number)
    {
        var clientId = Guid.NewGuid();
        _db.Clients.Add(new Client
        {
            Id = clientId,
            TenantId = _tenantId,
            CompanyId = Guid.NewGuid(),
            BranchId = branchId,
            Code = $"C{clientId.ToString()[..6]}",
            FirstName = "Test",
            LastName = "Client",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        });

        _db.Quotes.Add(new Quote
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            CompanyId = Guid.NewGuid(),
            BranchId = branchId,
            QuoteNumber = number,
            ClientId = clientId,
            Status = QuoteStatus.Pending,
            QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Subtotal = 100,
            Total = 115
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchVacio_DevuelveTodasLasSucursales()
    {
        var items = await _sut.GetFilteredAsync(null, null, null, null, null, Guid.Empty, 1, 20);
        var total = await _sut.GetFilteredCountAsync(null, null, null, null, null, Guid.Empty);

        Assert.Equal(3, items.Count);
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchConcreto_DevuelveSoloEsaSucursal()
    {
        var items = await _sut.GetFilteredAsync(null, null, null, null, null, _branchA, 1, 20);
        var total = await _sut.GetFilteredCountAsync(null, null, null, null, null, _branchA);

        Assert.Equal(2, items.Count);
        Assert.All(items, q => Assert.Equal(_branchA, q.BranchId));
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task GetFilteredAsync_ConBranchSinDatos_DevuelveVacio()
    {
        var items = await _sut.GetFilteredAsync(null, null, null, null, null, Guid.NewGuid(), 1, 20);

        Assert.Empty(items);
    }
}
