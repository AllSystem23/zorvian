using Xunit;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Infrastructure;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _basePath =
        Path.Combine(Path.GetTempPath(), "zorvian-storage-tests", Guid.NewGuid().ToString("N"));

    private LocalFileStorageService Sut() => new(_basePath);

    [Fact]
    public void GetSignedUrlAsync_BarePath_ReturnsRootedUrl()
    {
        var url = Sut().GetSignedUrlAsync("companies/x/logo.png").Result;
        Assert.Equal("/companies/x/logo.png", url);
    }

    [Fact]
    public void GetSignedUrlAsync_RootedPath_ReturnsRootedUrl()
    {
        var url = Sut().GetSignedUrlAsync("/companies/x/logo.png").Result;
        Assert.Equal("/companies/x/logo.png", url);
    }

    [Fact]
    public void GetSignedUrlAsync_GcsUrl_ExtractsObjectPath()
    {
        var gcs = "https://storage.googleapis.com/bucket/attendance/p/e/2026_in.jpg?X-Goog-Signature=abc";
        var url = Sut().GetSignedUrlAsync(gcs).Result;
        Assert.Equal("/attendance/p/e/2026_in.jpg", url);
    }

    [Fact]
    public void GetSignedUrlAsync_ExternalUrl_Passthrough()
    {
        const string external = "https://example.com/doc.pdf";
        var url = Sut().GetSignedUrlAsync(external).Result;
        Assert.Equal(external, url);
    }

    [Fact]
    public async Task UploadThenGetSignedUrl_RoundTrip()
    {
        var sut = Sut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var path = await sut.UploadFileAsync(stream, "companies/x/logo.png", "image/png");

        Assert.Equal("companies/x/logo.png", path);
        Assert.True(File.Exists(Path.Combine(_basePath, "companies", "x", "logo.png")));
        Assert.Equal("/companies/x/logo.png", await sut.GetSignedUrlAsync(path));
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesStoredFile()
    {
        var sut = Sut();
        using var stream = new MemoryStream(new byte[] { 9 });
        await sut.UploadFileAsync(stream, "permissions/abc.pdf", "application/pdf");

        await sut.DeleteFileAsync("permissions/abc.pdf");

        Assert.False(File.Exists(Path.Combine(_basePath, "permissions", "abc.pdf")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }
}

public sealed class FirebaseStorageServiceTests
{
    private FirebaseStorageService Sut() => new("test-bucket");

    [Fact]
    public void GetSignedUrlAsync_Null_ReturnsNull()
    {
        Assert.Null(Sut().GetSignedUrlAsync(null).Result);
    }

    [Fact]
    public void GetSignedUrlAsync_Empty_ReturnsEmpty()
    {
        Assert.Equal("", Sut().GetSignedUrlAsync("").Result);
    }

    [Fact]
    public void GetSignedUrlAsync_ExternalUrl_PassthroughWithoutSigning()
    {
        const string external = "https://example.com/doc.pdf";
        Assert.Equal(external, Sut().GetSignedUrlAsync(external).Result);
    }
}
