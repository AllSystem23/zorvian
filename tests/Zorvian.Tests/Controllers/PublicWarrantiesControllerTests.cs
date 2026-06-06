using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Web.Controllers;
using Xunit;

namespace Zorvian.Tests.Controllers;

public sealed class PublicWarrantiesControllerTests
{
    private readonly Mock<IWarrantyRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PublicWarrantiesController _sut;

    public PublicWarrantiesControllerTests()
    {
        _sut = new PublicWarrantiesController(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task Track_ValidWarrantyAndPhone_ReturnsOk()
    {
        // Arrange
        var warrantyNumber = "GAR-001";
        var phone = "12345678";
        var warranty = new Warranty 
        { 
            Id = Guid.NewGuid(),
            WarrantyNumber = warrantyNumber,
            Client = new Client { Phone = phone }
        };

        _repo.Setup(r => r.GetByWarrantyNumberAsync(warrantyNumber)).ReturnsAsync(warranty);
        _mapper.Setup(m => m.Map<WarrantyResponse>(warranty))
            .Returns(new WarrantyResponse(
                warranty.Id, warrantyNumber, Guid.Empty, "John", 
                Guid.Empty, "Laptop", null, null, null, null, null, null, 
                DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 
                12, "Terms", null, null, null, "active"));

        // Act
        var result = await _sut.Track(warrantyNumber, phone);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Track_InvalidPhone_ReturnsUnauthorized()
    {
        // Arrange
        var warrantyNumber = "GAR-001";
        var phone = "12345678";
        var warranty = new Warranty 
        { 
            Id = Guid.NewGuid(),
            WarrantyNumber = warrantyNumber,
            Client = new Client { Phone = "87654321" } // Wrong phone
        };

        _repo.Setup(r => r.GetByWarrantyNumberAsync(warrantyNumber)).ReturnsAsync(warranty);

        // Act
        var result = await _sut.Track(warrantyNumber, phone);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
