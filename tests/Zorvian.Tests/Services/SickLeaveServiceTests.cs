using Moq;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public class SickLeaveServiceTests
{
    private readonly Mock<ISickLeaveRepository> _mockRepo = new();
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo = new();
    private readonly SickLeaveService _sut;

    public SickLeaveServiceTests()
    {
        _sut = new SickLeaveService(_mockRepo.Object, _mockEmployeeRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddRecord()
    {
        // ARRANGE
        var request = new CreateSickLeaveRequest(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), "A01");
        
        // ACT
        var result = await _sut.CreateAsync(request);

        // ASSERT
        _mockRepo.Verify(r => r.AddAsync(It.Is<SickLeaveRecord>(sl => 
            sl.EmployeeId == request.EmployeeId && 
            sl.StartDate == request.StartDate && 
            sl.DiagnosisCode == request.DiagnosisCode)), Times.Once);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByEmployeeAsync_ShouldReturnRecords()
    {
        // ARRANGE
        var empId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByEmployeeAsync(empId))
            .ReturnsAsync(new List<SickLeaveRecord> { new SickLeaveRecord { EmployeeId = empId } });

        // ACT
        var result = await _sut.GetByEmployeeAsync(empId);

        // ASSERT
        Assert.Single(result);
    }
}
