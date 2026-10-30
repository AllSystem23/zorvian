using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.Documentary;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Jobs;

namespace Zorvian.Tests.Services;

/// <summary>
/// El HTML renderizado de cada versión (~40-80 KB) vive en Storage y la BD solo
/// guarda la referencia: (1) el resolvedor lee de la BD si aún lo hay y si no de
/// Storage, (2) la generación sube el archivo y deja Content vacío con fallback a
/// la BD si Storage falla, y (3) el job de backfill migra las filas antiguas por
/// lotes sin romper nada cuando Storage falla.
/// </summary>
public sealed class DocumentVersionContentStorageTests
{
    // ── Resolvedor ─────────────────────────────────────────────────────

    [Fact]
    public async Task Resolver_UsesDatabaseContent_WithoutTouchingStorage()
    {
        var storage = new Mock<IDocumentStorageService>();
        var sut = new DocumentVersionContentService(storage.Object, NullLogger<DocumentVersionContentService>.Instance);

        var content = await sut.GetContentAsync(Version(content: "<html>en BD</html>", path: "documents/a/v1.html"));

        Assert.Equal("<html>en BD</html>", content);
        storage.Verify(s => s.ReadTextFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Resolver_DownloadsFromStorage_WhenContentWasMigrated()
    {
        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.ReadTextFileAsync("documents/a/v1.html"))
            .ReturnsAsync("<html>desde storage</html>");
        var sut = new DocumentVersionContentService(storage.Object, NullLogger<DocumentVersionContentService>.Instance);

        var content = await sut.GetContentAsync(Version(content: "", path: "documents/a/v1.html"));

        Assert.Equal("<html>desde storage</html>", content);
    }

    [Fact]
    public async Task Resolver_ReturnsNull_WhenObjectIsMissing_OrStorageFails()
    {
        var missing = new Mock<IDocumentStorageService>();
        missing.Setup(s => s.ReadTextFileAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var broken = new Mock<IDocumentStorageService>();
        broken.Setup(s => s.ReadTextFileAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("bucket inaccesible"));

        var sutMissing = new DocumentVersionContentService(missing.Object, NullLogger<DocumentVersionContentService>.Instance);
        var sutBroken = new DocumentVersionContentService(broken.Object, NullLogger<DocumentVersionContentService>.Instance);

        // Un fallo de Storage no debe tumbar la petición: el endpoint responde 404.
        Assert.Null(await sutMissing.GetContentAsync(Version(content: "", path: "documents/a/v1.html")));
        Assert.Null(await sutBroken.GetContentAsync(Version(content: "", path: "documents/a/v1.html")));
    }

    [Fact]
    public async Task Resolver_ReturnsNull_WhenVersionHasNeitherContentNorPath()
    {
        var storage = new Mock<IDocumentStorageService>();
        var sut = new DocumentVersionContentService(storage.Object, NullLogger<DocumentVersionContentService>.Instance);

        Assert.Null(await sut.GetContentAsync(Version(content: "", path: "")));
        storage.Verify(s => s.ReadTextFileAsync(It.IsAny<string>()), Times.Never);
    }

    // ── Escritura ──────────────────────────────────────────────────────

    [Fact]
    public async Task Generate_UploadsHtml_AndLeavesOnlyTheReferenceInDb()
    {
        DocumentVersion? added = null;
        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string path, string _) => path);

        var sut = CreateDocumentService(storage.Object, out var docRepo);
        docRepo.Setup(r => r.AddVersionAsync(It.IsAny<DocumentVersion>()))
            .Callback<DocumentVersion>(v => added = v)
            .Returns(Task.CompletedTask);

        await sut.GenerateProfessionalDocumentAsync(Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, string>());

        Assert.NotNull(added);
        Assert.Equal(string.Empty, added!.Content);          // el HTML NO viaja a la BD
        Assert.StartsWith("documents/", added.FilePath);     // pero sí la ruta
        Assert.True(added.FileSizeBytes > 0);
        Assert.False(string.IsNullOrEmpty(added.FileHash));
    }

    [Fact]
    public async Task Generate_KeepsHtmlInDb_WhenStorageFails()
    {
        DocumentVersion? added = null;
        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("53100: storage lleno"));

        var sut = CreateDocumentService(storage.Object, out var docRepo);
        docRepo.Setup(r => r.AddVersionAsync(It.IsAny<DocumentVersion>()))
            .Callback<DocumentVersion>(v => added = v)
            .Returns(Task.CompletedTask);

        await sut.GenerateProfessionalDocumentAsync(Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, string>());

        // Fallback: PDF/verificación/constancia siguen funcionando desde la BD.
        Assert.False(string.IsNullOrEmpty(added!.Content));
        Assert.Contains("<html>", added.Content);
    }

    // ── Backfill (job) ─────────────────────────────────────────────────

    [Fact]
    public async Task Sweep_MigratesBatchAndClearsContent()
    {
        var db = CreateDb();
        db.DocumentVersions.AddRange(
            Version(content: "<html>A</html>", path: "documents/a/v1.html"),
            Version(content: "<html>B</html>", path: "documents/b/v1.html"),
            Version(content: "", path: "documents/c/v1.html"));   // ya migrada
        await db.SaveChangesAsync();

        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string path, string _) => path);

        await CreateSweep(db, storage.Object).RunAsync();

        var rows = db.DocumentVersions.IgnoreQueryFilters().ToList();
        // 2 recién migradas + la que ya estaba vacía.
        Assert.Equal(3, rows.Count(r => r.Content == ""));
        Assert.Equal("", rows.Single(r => r.FilePath == "documents/a/v1.html").Content);
        Assert.Equal("", rows.Single(r => r.FilePath == "documents/b/v1.html").Content);
        // Solo 2 subidas: la fila que ya estaba vacía no se vuelve a subir.
        storage.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        storage.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), "documents/c/v1.html", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Sweep_KeepsFailingRowsIntact_AndMigratesTheRest()
    {
        var db = CreateDb();
        db.DocumentVersions.AddRange(
            Version(content: "<html>ok</html>", path: "documents/ok/v1.html"),
            Version(content: "<html>boom</html>", path: "documents/boom/v1.html"));
        await db.SaveChangesAsync();

        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string path, string _) =>
                path.Contains("boom") ? throw new Exception("storage caído") : path);

        await CreateSweep(db, storage.Object).RunAsync();

        var rows = db.DocumentVersions.IgnoreQueryFilters().ToList();
        // La que falló conserva su contenido para reintentarse en la próxima corrida.
        Assert.Equal("<html>boom</html>", rows.Single(r => r.FilePath == "documents/boom/v1.html").Content);
        Assert.Equal("", rows.Single(r => r.FilePath == "documents/ok/v1.html").Content);
    }

    [Fact]
    public async Task Sweep_IsNoop_WhenNothingIsPending()
    {
        var db = CreateDb();
        var storage = new Mock<IDocumentStorageService>();
        var background = new Mock<IBackgroundJobClient>();

        await CreateSweep(db, storage.Object, background.Object).RunAsync();

        storage.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        background.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task Sweep_ChainsAnotherRun_WhenTheBatchIsFull()
    {
        var db = CreateDb();
        for (var i = 0; i < DocumentVersionContentSweepJob.BatchSize; i++)
            db.DocumentVersions.Add(
                Version(content: $"<html>{i}</html>", path: $"documents/{i}/v1.html"));
        await db.SaveChangesAsync();

        var storage = new Mock<IDocumentStorageService>();
        storage.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Stream _, string path, string _) => path);
        var background = new Mock<IBackgroundJobClient>();

        await CreateSweep(db, storage.Object, background.Object).RunAsync();

        Assert.All(db.DocumentVersions.IgnoreQueryFilters().ToList(), v => Assert.Equal("", v.Content));
        background.Verify(b => b.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static DocumentVersion Version(string content, string path) => new()
    {
        Id = Guid.NewGuid(),
        DocumentId = Guid.NewGuid(),
        VersionNumber = 1,
        Content = content,
        FilePath = path,
        TenantId = "SYSTEM",
        CreatedAt = DateTime.UtcNow,
    };

    private static ZorvianDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        return new ZorvianDbContext(options, tenant.Object);
    }

    private static DocumentVersionContentSweepJob CreateSweep(
        ZorvianDbContext db,
        IDocumentStorageService storage,
        IBackgroundJobClient? backgroundJobs = null) =>
        new(db, storage, backgroundJobs ?? new Mock<IBackgroundJobClient>().Object,
            NullLogger<DocumentVersionContentSweepJob>.Instance);

    private static DocumentService CreateDocumentService(
        IDocumentStorageService storage,
        out Mock<IGeneratedDocumentRepository> docRepo)
    {
        var templateRepo = new Mock<IDocumentTemplateRepository>();
        templateRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Constancia de Empleo",
            Category = "HR",
            Module = "Employee",
            CountryCode = "ALL",
            Content = "<html><body><p>Documento</p></body></html>",
        });

        docRepo = new Mock<IGeneratedDocumentRepository>();
        docRepo.Setup(r => r.AddAsync(It.IsAny<GeneratedDocument>())).Returns(Task.CompletedTask);
        docRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<string>())).ReturnsAsync(new Company
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Comercial Zorvian",
            TaxId = "J0310000000000",
            Country = "Nicaragua",
            Address = "Central, Managua",
        });

        var appLinks = new Mock<IAppLinks>();
        appLinks.Setup(a => a.BuildDocumentVerifyUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://app.example.com/documents/verify/{id}");

        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns("tenant-1");

        return new DocumentService(
            templateRepo.Object,
            docRepo.Object,
            new Mock<IAiDocumentService>().Object,
            companyRepo.Object,
            appLinks.Object,
            tenant.Object,
            storage,
            new Mock<IDocumentVersionContentService>().Object,
            NullLogger<DocumentService>.Instance);
    }
}
