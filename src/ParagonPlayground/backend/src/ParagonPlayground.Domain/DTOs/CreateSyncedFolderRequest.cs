namespace ParagonPlayground.Domain.DTOs;

/// <summary>Represents a request to sync a folder from a sharepoint site.</summary>
public class CreateSyncedFolderRequest
{
  /// <summary>The sharepoint id for the folder that will be synced.</summary>
  public required string SharePointFolderId { get; set; }

  /// <summary>The id of the sharepoint site where the folder to be synced exists.</summary>
  public required string SharePointSiteId { get; set; }

  /// <summary>The id of the parent id for the folder being synced.</summary>
  public string ParentId { get; set; } = string.Empty;

  /// <summary>The associated credentials with the folder sync.</summary>
  public string CredentialId { get; set; } = string.Empty;
}