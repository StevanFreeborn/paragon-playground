namespace ParagonPlayground.Domain.Entities;

/// <summary>Maps an app user to their Paragon integration credential.</summary>
public class UserCredential
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>App user who owns this credential.</summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>Organization the user belongs to.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Paragon credential ID from the integration install flow.</summary>
  public string CredentialId { get; set; } = string.Empty;

  /// <summary>Integration type (e.g. "sharepoint").</summary>
  public string IntegrationType { get; set; } = string.Empty;

  /// <summary>Timestamp when the credential was connected.</summary>
  public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
