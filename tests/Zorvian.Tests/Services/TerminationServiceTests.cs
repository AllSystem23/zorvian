using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public class TerminationServiceTests
{
    private readonly Mock<ITerminationRepository> _mockRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly Mock<IBenefitProvisionRepository> _mockBenefitRepo = new();
    private readonly TerminationService _sut;

    public TerminationServiceTests()
    {
        _sut = new TerminationService(_mockRepo.Object, _mockEmployeeRepo.Object, _mockBenefitRepo.Object);
    }

    [Fact]
    public async Task CalculateAsync_ShouldCalculateCorrectSeverance()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        var terminationDate = new DateOnly(2026, 6, 1);
        var hireDate = new DateOnly(2023, 6, 1); // 3 years
        var salary = 3000m; // 100 per day
        
        _mockEmployeeRepo.Setup(r => r.GetByIdAsync(empId))
            .ReturnsAsync(new Employee { Id = empId, Salary = salary, HireDate = hireDate });
        
        _mockBenefitRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<BenefitProvision> {
                new BenefitProvision { BenefitType = "vacation", Amount = 500m },
                new BenefitProvision { BenefitType = "aguinaldo", Amount = 300m }
            });

        // ACT
        var result = await _sut.CalculateAsync(empId, TerminationReason.VoluntaryResignation, terminationDate);

        // ASSERT
        Assert.NotNull(result);
        // 3 years * 30 days = 90 days. 90 days * 100/day = 9000
        Assert.Equal(9000m + 500m + 300m, result.GrossSettlement);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<TerminationRecord>()), Times.Once);
    }
}
