namespace ParagonPlayground.Domain.Entities;

/// <summary>Represents an authenticated user session.</summary>
public class Session
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Identifier of the user this session belongs to.</summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>Hashed authentication token.</summary>
  public string TokenHash { get; set; } = string.Empty;

  /// <summary>Timestamp when the session was created.</summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>Timestamp when the session expires and becomes invalid.</summary>
  public DateTime ExpiresAt { get; set; }
}