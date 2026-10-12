using Xunit;
using Zorvian.Application.Helpers;

namespace Zorvian.Tests.Helpers;

public sealed class StoragePathHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractStoragePath_NullOrEmpty_ReturnsNull(string? value)
    {
        Assert.Null(StoragePathHelper.ExtractStoragePath(value));
    }

    [Fact]
    public void ExtractStoragePath_PublicGcsUrl_ReturnsObjectPath()
    {
        var url = "https://storage.googleapis.com/zorvian-erp.firebasestorage.app/companies/abc/logo.png";
        Assert.Equal("companies/abc/logo.png", StoragePathHelper.ExtractStoragePath(url));
    }

    [Fact]
    public void ExtractStoragePath_SignedGcsUrl_StripsQueryString()
    {
        var url = "https://storage.googleapis.com/zorvian-erp.firebasestorage.app/attendance/photos/e1/2026_in.jpg" +
                  "?X-Goog-Algorithm=GOOG4-RSA-SHA256&X-Goog-Expires=900&X-Goog-Signature=deadbeef";
        Assert.Equal("attendance/photos/e1/2026_in.jpg", StoragePathHelper.ExtractStoragePath(url));
    }

    [Theory]
    [InlineData("/permissions/abc.pdf", "permissions/abc.pdf")]
    [InlineData("companies/abc/logo.png", "companies/abc/logo.png")]
    public void ExtractStoragePath_LocalOrBarePath_ReturnsTrimmedPath(string value, string expected)
    {
        Assert.Equal(expected, StoragePathHelper.ExtractStoragePath(value));
    }

    [Theory]
    [InlineData("https://example.com/doc.pdf")]
    [InlineData("http://test.com/doc.pdf")]
    [InlineData("https://firebasestorage.googleapis.com/v0/b/bucket/o/x?alt=media&token=1")]
    public void ExtractStoragePath_ExternalUrl_ReturnsNull(string value)
    {
        Assert.Null(StoragePathHelper.ExtractStoragePath(value));
    }
}
