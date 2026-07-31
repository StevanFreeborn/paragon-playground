namespace ParagonPlayground.Domain.DTOs;

/// <summary>File or folder returned by the storage API.</summary>
public class StorageItemResponse
{
  /// <summary>Unique identifier.</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Display name.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>True for folders, false for files.</summary>
  public bool IsFolder { get; set; }

  /// <summary>Parent folder ID (null for root items).</summary>
  public string? ParentId { get; set; }

  /// <summary>MIME type (null for folders).</summary>
  public string? ContentType { get; set; }

  /// <summary>File size in bytes (0 for folders).</summary>
  public long FileSize { get; set; }

  /// <summary>Direct SharePoint web URL (null for folders).</summary>
  public string? SharePointWebUrl { get; set; }

  /// <summary>User who created this item.</summary>
  public string CreatedByUserId { get; set; } = string.Empty;

  /// <summary>Display name of the creator.</summary>
  public string CreatedByDisplayName { get; set; } = string.Empty;

  /// <summary>Timestamp when the item was created.</summary>
  public DateTime CreatedAt { get; set; }
}
