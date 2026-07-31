namespace ParagonPlayground.Domain.DTOs;

/// <summary>Request to store a new Paragon credential mapping.</summary>
public class CredentialRequest
{
  /// <summary>Paragon credential ID from the integration install flow.</summary>
  public string CredentialId { get; set; } = string.Empty;

  /// <summary>Integration type (e.g. "sharepoint").</summary>
  public string IntegrationType { get; set; } = string.Empty;
}