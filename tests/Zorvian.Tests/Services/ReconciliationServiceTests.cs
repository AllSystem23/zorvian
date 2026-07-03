using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Services;
using Zorvian.Core.Entities;
using System.Text;

namespace Zorvian.Tests.Services;

public class ReconciliationServiceTests
{
    private readonly Mock<IPayrollRepository> _mockRepo = new();
    private readonly ReconciliationService _sut;

    public ReconciliationServiceTests()
    {
        _sut = new ReconciliationService(_mockRepo.Object);
    }

    [Fact]
    public async Task ProcessBankResponseFileAsync_ShouldReconcileValidPayments()
    {
        // ARRANGE
        var csvContent = "SIM-12345678,ok\nSIM-87654321,ok";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        
        _mockRepo.Setup(r => r.GetDetailByReferenceAsync("SIM-12345678"))
            .ReturnsAsync(new PayrollDetail { PaymentReference = "SIM-12345678", PaymentStatus = "pending" });
        _mockRepo.Setup(r => r.GetDetailByReferenceAsync("SIM-87654321"))
            .ReturnsAsync(new PayrollDetail { PaymentReference = "SIM-87654321", PaymentStatus = "pending" });

        // ACT
        var (reconciled, failed, message) = await _sut.ProcessBankResponseFileAsync(stream);

        // ASSERT
        Assert.Equal(2, reconciled);
        Assert.Equal(0, failed);
        _mockRepo.Verify(r => r.UpdateDetailAsync(It.IsAny<PayrollDetail>()), Times.Exactly(2));
        _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessBankResponseFileAsync_ShouldMarkFailedIfBankReportsError()
    {
        // ARRANGE
        var csvContent = "SIM-12345678,failed,Account Closed";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        
        _mockRepo.Setup(r => r.GetDetailByReferenceAsync("SIM-12345678"))
            .ReturnsAsync(new PayrollDetail { PaymentReference = "SIM-12345678", PaymentStatus = "pending" });

        // ACT
        var (reconciled, failed, message) = await _sut.ProcessBankResponseFileAsync(stream);

        // ASSERT
        Assert.Equal(1, reconciled);
        Assert.Equal(0, failed); // reconciled count still increments for processed lines
        _mockRepo.Verify(r => r.UpdateDetailAsync(It.Is<PayrollDetail>(d => d.PaymentStatus == "failed")), Times.Once);
    }

    [Fact]
    public async Task ProcessBankResponseFileAsync_ShouldCountFailedIfNotFound()
    {
        // ARRANGE
        var csvContent = "UNKNOWN-REF,ok";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        
        _mockRepo.Setup(r => r.GetDetailByReferenceAsync("UNKNOWN-REF"))
            .ReturnsAsync((PayrollDetail?)null);

        // ACT
        var (reconciled, failed, message) = await _sut.ProcessBankResponseFileAsync(stream);

        // ASSERT
        Assert.Equal(0, reconciled);
        Assert.Equal(1, failed);
    }
}
