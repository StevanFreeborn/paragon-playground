namespace ParagonPlayground.Domain.Entities;

/// <summary>Represents a registered user within an organization.</summary>
public class User
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>User's email address (used as login).</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>Display name shown in the UI.</summary>
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>BCrypt hash of the user's password.</summary>
  public string PasswordHash { get; set; } = string.Empty;

  /// <summary>Identifier of the organization this user belongs to.</summary>
  public string OrganizationId { get; set; } = string.Empty;

  /// <summary>Timestamp when the user was created.</summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}