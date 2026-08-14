using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace ParagonPlayground.Domain.DTOs;

/// <summary>Response payload returned by the Paragon Sync API GET /api/syncs/{syncId}/records endpoint.</summary>
public class SyncedRecordsResponse
{
  /// <summary>Synced records returned for the requested page.</summary>
  [JsonPropertyName("data")]
  public Collection<SyncedRecordItem> Data { get; init; } = [];

  /// <summary>Paging metadata used to continue retrieving additional records.</summary>
  [JsonPropertyName("paging")]
  public SyncedRecordsPaging Paging { get; set; } = new();
}


/// <summary>Pagination metadata returned with synced record batches.</summary>
public class SyncedRecordsPaging
{
  /// <summary>Total number of records known to the sync.</summary>
  [JsonPropertyName("totalRecords")]
  public int TotalRecords { get; set; }

  /// <summary>Total number of active (non-deleted) records in the sync.</summary>
  [JsonPropertyName("totalActiveRecords")]
  public int TotalActiveRecords { get; set; }

  /// <summary>Count of records still remaining after the current page.</summary>
  [JsonPropertyName("remainingRecords")]
  public int RemainingRecords { get; set; }

  /// <summary>Cursor to request the next page of records.</summary>
  [JsonPropertyName("cursor")]
  public string? Cursor { get; set; }

  /// <summary>Unix timestamp of the latest record observed in this page.</summary>
  [JsonPropertyName("lastSeen")]
  public long LastSeen { get; set; }
}


/// <summary>Represents a single synced file or record item returned by Paragon Managed Sync.</summary>
public class SyncedRecordItem
{
  /// <summary>Paragon-generated sync record identifier.</summary>
  [JsonPropertyName("id")]
  public string Id { get; set; } = string.Empty;

  /// <summary>Provider-native record identifier (for example, SharePoint drive item ID).</summary>
  [JsonPropertyName("externalId")]
  public string ExternalId { get; set; } = string.Empty;

  /// <summary>Display name for the synced file or folder.</summary>
  [JsonPropertyName("name")]
  public string Name { get; set; } = "Untitled Item";

  /// <summary>MIME type of the synced file; empty for folders.</summary>
  [JsonPropertyName("mimeType")]
  public string MimeType { get; set; } = "application/octet-stream";

  /// <summary>File size in bytes.</summary>
  [JsonPropertyName("size")]
  public long Size { get; set; }

  /// <summary>Provider URL for viewing the record in the source system.</summary>
  [JsonPropertyName("url")]
  public string Url { get; set; } = string.Empty;

  /// <summary>External identifier of the record's parent folder.</summary>
  [JsonPropertyName("parentFolderId")]
  public string ParentFolderId { get; set; } = string.Empty;

  /// <summary>Integration-specific metadata attached to the synced record.</summary>
  [JsonPropertyName("customFields")]
  public Dictionary<string, object>? CustomFields { get; init; }

  /// <summary>Determines whether the synced item represents a folder.</summary>
  public bool IsFolder()
  {
    return string.IsNullOrEmpty(MimeType);
  }
}