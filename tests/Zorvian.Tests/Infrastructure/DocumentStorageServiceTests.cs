using Xunit;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Tests.Infrastructure;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _basePath =
        Path.Combine(Path.GetTempPath(), "zorvian-storage-tests", Guid.NewGuid().ToString("N"));

    private LocalFileStorageService Sut() => new(_basePath);

    [Fact]
    public async Task GetSignedUrlAsync_BarePath_ReturnsRootedUrl()
    {
        var url = await Sut().GetSignedUrlAsync("companies/x/logo.png");
        Assert.Equal("/companies/x/logo.png", url);
    }

    [Fact]
    public async Task GetSignedUrlAsync_RootedPath_ReturnsRootedUrl()
    {
        var url = await Sut().GetSignedUrlAsync("/companies/x/logo.png");
        Assert.Equal("/companies/x/logo.png", url);
    }

    [Fact]
    public async Task GetSignedUrlAsync_GcsUrl_ExtractsObjectPath()
    {
        var gcs = "https://storage.googleapis.com/bucket/attendance/p/e/2026_in.jpg?X-Goog-Signature=abc";
        var url = await Sut().GetSignedUrlAsync(gcs);
        Assert.Equal("/attendance/p/e/2026_in.jpg", url);
    }

    [Fact]
    public async Task GetSignedUrlAsync_ExternalUrl_Passthrough()
    {
        const string external = "https://example.com/doc.pdf";
        var url = await Sut().GetSignedUrlAsync(external);
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

    [Fact]
    public async Task ListFilesAsync_ReturnsOnlyObjectsUnderThePrefix_AsRelativePaths()
    {
        var sut = Sut();
        await using (var a = new MemoryStream(new byte[] { 1 }))
            await sut.UploadFileAsync(a, "companies/a/logo.png", "image/png");
        await using (var b = new MemoryStream(new byte[] { 2 }))
            await sut.UploadFileAsync(b, "attendance/photos/x/in.jpg", "image/jpeg");

        var objects = await sut.ListFilesAsync("companies/");

        Assert.Single(objects);
        Assert.Equal("companies/a/logo.png", objects[0].Path);
        Assert.True(objects[0].LastModifiedUtc > DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task ListFilesAsync_MissingPrefix_ReturnsEmpty()
    {
        Assert.Empty(await Sut().ListFilesAsync("companies/"));
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
    public async Task GetSignedUrlAsync_Null_ReturnsNull()
    {
        Assert.Null(await Sut().GetSignedUrlAsync(null));
    }

    [Fact]
    public async Task GetSignedUrlAsync_Empty_ReturnsEmpty()
    {
        Assert.Equal("", await Sut().GetSignedUrlAsync(""));
    }

    [Fact]
    public async Task GetSignedUrlAsync_ExternalUrl_PassthroughWithoutSigning()
    {
        const string external = "https://example.com/doc.pdf";
        Assert.Equal(external, await Sut().GetSignedUrlAsync(external));
    }
}
