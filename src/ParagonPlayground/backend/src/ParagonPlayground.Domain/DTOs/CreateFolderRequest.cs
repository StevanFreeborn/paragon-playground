namespace ParagonPlayground.Domain.DTOs;

/// <summary>Request to create a new virtual folder.</summary>
public class CreateFolderRequest
{
  /// <summary>Folder name.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>Optional parent folder ID (null for root).</summary>
  public string? ParentId { get; set; }
}
