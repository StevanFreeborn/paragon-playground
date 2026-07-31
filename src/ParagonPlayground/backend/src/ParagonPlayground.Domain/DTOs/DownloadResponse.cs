namespace ParagonPlayground.Domain.DTOs;

/// <summary>Download URLs for a stored file.</summary>
public class DownloadResponse
{
  /// <summary>Direct SharePoint web URL (opens in SharePoint).</summary>
  public string? SharePointUrl { get; set; }

  /// <summary>App-proxied download URL (streams through the backend via Paragon).</summary>
  public string? ProxyUrl { get; set; }
}
