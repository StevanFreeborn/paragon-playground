namespace ParagonPlayground.Domain.Entities;

/// <summary>Represents a tenant organization in the system.</summary>
public class Organization
{
  /// <summary>Unique identifier (MongoDB ObjectId).</summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>Display name of the organization.</summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>URL-friendly slug used for subdomain routing.</summary>
  public string Slug { get; set; } = string.Empty;

  /// <summary>Timestamp when the organization was created.</summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}