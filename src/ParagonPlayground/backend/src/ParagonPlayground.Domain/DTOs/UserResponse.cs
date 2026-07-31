namespace ParagonPlayground.Domain.DTOs;

/// <summary>Public user profile returned by the API.</summary>
public class UserResponse
{
  /// <summary>Unique identifier of the user.</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>User's email address.</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>Display name shown in the UI.</summary>
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>Identifier of the user's organization.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Display name of the user's organization.</summary>
  public string OrganizationName { get; set; } = string.Empty;

  /// <summary>URL-friendly slug of the user's organization.</summary>
  public string OrganizationSlug { get; set; } = string.Empty;

  /// <summary>User's role within the organization ("admin" or "member").</summary>
  public string Role { get; set; } = "member";
}