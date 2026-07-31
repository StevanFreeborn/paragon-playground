namespace ParagonPlayground.Domain.DTOs;

/// <summary>Stored Paragon credential for the current user.</summary>
public class CredentialResponse
{
  /// <summary>Unique identifier.</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Paragon credential ID.</summary>
  public string CredentialId { get; set; } = string.Empty;

  /// <summary>Integration type (e.g. "sharepoint").</summary>
  public string IntegrationType { get; set; } = string.Empty;

  /// <summary>Timestamp when the credential was connected.</summary>
  public DateTime ConnectedAt { get; set; }
}