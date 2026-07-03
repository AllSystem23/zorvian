using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Models;

using Zorvian.Infrastructure.Data;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class PayrollLocalizationServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<IEmployeePayrollExemptionRepository> _exemptionRepoMock;
    private readonly PayrollLocalizationService _sut;

    public PayrollLocalizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _db = new ZorvianDbContext(options, tenantMock.Object);
        
        _exemptionRepoMock = new Mock<IEmployeePayrollExemptionRepository>();
        _sut = new PayrollLocalizationService(_exemptionRepoMock.Object);
    }

    [Fact]
    public async Task CalculateConceptAsync_Should_Return_Zero_If_Exempt()
    {
        var concept = new PayrollConcept { Id = Guid.NewGuid(), Code = "INSS_LAB", CalculationFormula = "Salary * 0.07" };
        var employeeId = Guid.NewGuid();
        _exemptionRepoMock.Setup(r => r.IsExemptAsync(employeeId, concept.Id)).ReturnsAsync(true);

        var result = await _sut.CalculateConceptAsync(concept, 10000m, new PayrollContext(10, false), employeeId);

        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task CalculateConceptAsync_Should_Calculate_Correctly_Based_On_EmployeeCount()
    {
        var concept = new PayrollConcept { Id = Guid.NewGuid(), Code = "INSS_PAT", CalculationFormula = "Salary * 0.225" };
        var employeeId = Guid.NewGuid();
        _exemptionRepoMock.Setup(r => r.IsExemptAsync(employeeId, concept.Id)).ReturnsAsync(false);

        // < 50 employees -> 21.5%
        var resultSmall = await _sut.CalculateConceptAsync(concept, 10000m, new PayrollContext(10, false), employeeId);
        Assert.Equal(2150m, resultSmall);

        // >= 50 employees -> 22.5%
        var resultLarge = await _sut.CalculateConceptAsync(concept, 10000m, new PayrollContext(60, false), employeeId);
        Assert.Equal(2250m, resultLarge);
    }

    [Fact]
    public async Task CalculateConceptAsync_Should_Return_Zero_For_Indemnity_On_Resignation()
    {
        var concept = new PayrollConcept { Id = Guid.NewGuid(), Code = "INDEM_ANT", CalculationFormula = "Salary * 1" };
        var employeeId = Guid.NewGuid();
        _exemptionRepoMock.Setup(r => r.IsExemptAsync(employeeId, concept.Id)).ReturnsAsync(false);

        var context = new PayrollContext(10, false, new TerminationContext(TerminationReason.VoluntaryResignation, DateTime.Now.AddYears(-1), DateTime.Now, 1000m));

        var result = await _sut.CalculateConceptAsync(concept, 1000m, context, employeeId);

        Assert.Equal(0m, result);
    }
}
