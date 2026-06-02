using Moq;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Application.Jobs;
using Xunit;
using System.Net;
using System.Net.Http;
using Moq.Protected;

namespace Zorvian.Tests.Jobs;

public sealed class OcrProcessingJobTests
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<IOcrService> _ocrService = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();

    public OcrProcessingJobTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantMock = new Mock<Zorvian.Core.Interfaces.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns("tenant-1");
        _db = new ZorvianDbContext(options, tenantMock.Object);
    }

    [Fact]
    public async Task RunAsync_ProcessesDocumentAndUpdatesOcrResult()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new PermissionRequest
        {
            Id = permissionId,
            SupportingDocumentUrl = "http://test.com/doc.pdf",
            TenantId = "tenant-1"
        };
        _db.PermissionRequests.Add(permission);
        await _db.SaveChangesAsync();

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("dummy content")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        _ocrService.Setup(s => s.ExtractTextAsync(It.IsAny<Stream>()))
            .ReturnsAsync("Extracted Text: Medical Certificate");

        var repoMock = new Mock<Zorvian.Application.Interfaces.IPermissionRepository>();
        repoMock.Setup(r => r.GetByIdAsync(permissionId)).ReturnsAsync(permission);
        
        var sut = new OcrProcessingJob(repoMock.Object, _ocrService.Object, _httpClientFactory.Object);

        // Act
        await sut.RunAsync(permissionId);

        // Assert
        Assert.Equal("Extracted Text: Medical Certificate", permission.OcrResult);
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
