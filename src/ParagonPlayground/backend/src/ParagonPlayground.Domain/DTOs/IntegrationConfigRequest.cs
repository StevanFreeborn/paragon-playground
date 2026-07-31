namespace ParagonPlayground.Domain.DTOs;

/// <summary>Request to create or update the organization's integration configuration.</summary>
public class IntegrationConfigRequest
{
  /// <summary>Connection mode: "default" (ISV-provided app) or "byo" (user-configured OAuth).</summary>
  public string ConnectionMode { get; set; } = "default";

  /// <summary>Target SharePoint site URL (e.g. https://contoso.sharepoint.com/sites/MySite).</summary>
  public string? SharePointSiteUrl { get; set; }

  /// <summary>Target folder path within the SharePoint site.</summary>
  public string? SharePointFolderPath { get; set; }
}
