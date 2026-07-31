namespace ParagonPlayground.Domain.Entities;

/// <summary>Per-organization Paragon/SharePoint integration configuration.</summary>
public class OrganizationIntegration
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Organization this config belongs to.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Connection mode: "default" (ISV-provided app) or "byo" (user-configured OAuth).</summary>
  public string ConnectionMode { get; set; } = "default";

  /// <summary>Full SharePoint site URL (e.g. https://contoso.sharepoint.com/sites/MySite).</summary>
  public string? SharePointSiteUrl { get; set; }

  /// <summary>Resolved SharePoint site ID (e.g. contoso.sharepoint.com,guid,guid).</summary>
  public string? SharePointSiteId { get; set; }

  /// <summary>Target folder path within the SharePoint site.</summary>
  public string? SharePointFolderPath { get; set; }

  /// <summary>Timestamp of the last configuration update.</summary>
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}