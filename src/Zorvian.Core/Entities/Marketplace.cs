namespace Zorvian.Core.Entities;

/// <summary>
/// Marketplace module/plugin definition (P4.1)
/// Allows third-party developers to extend Zorvian ERP
/// </summary>
public class MarketplaceModule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public string? AuthorUrl { get; set; }
    public string? IconUrl { get; set; }
    public string Category { get; set; } = string.Empty; // accounting, hr, sales, etc.
    public decimal Price { get; set; } = 0;
    public bool IsOfficial { get; set; } = false;
    public bool IsPublished { get; set; } = false;
    public int Downloads { get; set; } = 0;
    public double Rating { get; set; } = 0;
    public string Status { get; set; } = "draft"; // draft, published, deprecated
    public string? ManifestUrl { get; set; }
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// Tenant installation of a marketplace module
/// </summary>
public class ModuleInstallation : BaseEntity
{
    public Guid ModuleId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active, suspended, uninstalled
    public string? Configuration { get; set; } // JSON config
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Module review and rating
/// </summary>
public class ModuleReview : BaseEntity
{
    public Guid ModuleId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
}
