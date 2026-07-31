namespace ParagonPlayground.Domain.DTOs;

/// <summary>Organization's Paragon/SharePoint integration configuration.</summary>
public class IntegrationConfigResponse
{
  /// <summary>Unique identifier.</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Organization this config belongs to.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Connection mode: "default" (ISV-provided app) or "byo" (user-configured OAuth).</summary>
  public string ConnectionMode { get; set; } = "default";

  /// <summary>Full SharePoint site URL (e.g. https://contoso.sharepoint.com/sites/MySite).</summary>
  public string? SharePointSiteUrl { get; set; }

  /// <summary>Resolved SharePoint site ID (e.g. contoso.sharepoint.com,guid,guid).</summary>
  public string? SharePointSiteId { get; set; }

  /// <summary>Target folder path within SharePoint.</summary>
  public string? SharePointFolderPath { get; set; }

  /// <summary>Timestamp of last update.</summary>
  public DateTime UpdatedAt { get; set; }
}
