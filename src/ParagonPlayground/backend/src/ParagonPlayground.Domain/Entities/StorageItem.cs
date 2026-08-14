namespace ParagonPlayground.Domain.Entities;

/// <summary>Represents a file or folder in the virtual storage tree (independent of SharePoint structure).</summary>
public class StorageItem
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Organization this item belongs to.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Display name of the file or folder.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>True if this is a folder, false if it's a file.</summary>
  public bool IsFolder { get; set; }

  /// <summary>Parent folder ID (null for root-level items).</summary>
  public string? ParentId { get; set; }

  /// <summary>MIME type of the file (null for folders).</summary>
  public string? ContentType { get; set; }

  /// <summary>File size in bytes (0 for folders).</summary>
  public long FileSize { get; set; }


  /// <summary>SharePoint site ID where the file was uploaded (null for folders).</summary>
  public string? SharePointSiteId { get; set; }

  /// <summary>SharePoint drive item ID (null for folders).</summary>
  public string? SharePointDriveItemId { get; set; }

  /// <summary>SharePoint web URL for direct access (null for folders).</summary>
  public string? SharePointWebUrl { get; set; }


  /// <summary>Indicates whether the storage item is associated with a managed sync</summary>
  public bool IsManagedSync { get; set; }

  /// <summary>Identifies the managed sync this storage item is attached to.</summary>
  public string ManagedSyncId { get; set; } = string.Empty;

  /// <summary>Paragon Sync record ID (sync-generated UUID) for items ingested from managed sync.</summary>
  public string ParagonRecordId { get; set; } = string.Empty;

  /// <summary>Identifies the sharepoint folder id that the managed sync is attached to.</summary>
  public string SharePointFolderId { get; set; } = string.Empty;

  /// <summary>Indicates whether the storage item can be edited or modified by our application.</summary>
  public bool IsReadOnly { get; set; }


  /// <summary>User who created this item.</summary>
  public string CreatedByUserId { get; set; } = string.Empty;

  /// <summary>Timestamp when the item was created.</summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}