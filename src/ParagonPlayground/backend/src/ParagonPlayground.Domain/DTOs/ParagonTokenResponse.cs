namespace ParagonPlayground.Domain.DTOs;

/// <summary>Response containing a signed Paragon JWT and project ID.</summary>
public class ParagonTokenResponse
{
  /// <summary>Signed JWT for authenticating with the Paragon SDK.</summary>
  public string ParagonJwt { get; set; } = string.Empty;

  /// <summary>Paragon project ID for SDK initialization.</summary>
  public string ProjectId { get; set; } = string.Empty;
}