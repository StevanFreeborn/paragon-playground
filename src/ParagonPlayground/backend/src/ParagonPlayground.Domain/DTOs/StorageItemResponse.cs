using ParagonPlayground.Domain.Entities;

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

  /// <summary>Indicates if the storage item is associated with a managed sync.</summary>
  public bool IsManagedSync { get; set; }

  /// <summary>Indicates if the storage item can be edited by our application.</summary>
  public bool IsReadOnly { get; set; }

  /// <summary>User who created this item.</summary>
  public string CreatedByUserId { get; set; } = string.Empty;

  /// <summary>Display name of the creator.</summary>
  public string CreatedByDisplayName { get; set; } = string.Empty;

  /// <summary>Timestamp when the item was created.</summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>Creates a response DTO from a storage item entity.</summary>
  public static StorageItemResponse From(StorageItem item, string createdByDisplayName)
  {
    ArgumentNullException.ThrowIfNull(item);
    ArgumentNullException.ThrowIfNull(createdByDisplayName);

    return new StorageItemResponse
    {
      Id = item.Id,
      Name = item.Name,
      IsFolder = item.IsFolder,
      ParentId = item.ParentId,
      ContentType = item.ContentType,
      FileSize = item.FileSize,
      SharePointWebUrl = item.SharePointWebUrl,
      IsManagedSync = item.IsManagedSync,
      IsReadOnly = item.IsReadOnly,
      CreatedByUserId = item.CreatedByUserId,
      CreatedByDisplayName = createdByDisplayName,
      CreatedAt = item.CreatedAt,
    };
  }
}