namespace ParagonPlayground.Infrastructure.Services;

/// <summary>Provides password hashing and verification using BCrypt.</summary>
public class PasswordService
{
  /// <summary>Hashes a plain-text password.</summary>
  public string Hash(string password)
  {
    return BCrypt.Net.BCrypt.HashPassword(password);
  }

  /// <summary>Verifies a plain-text password against a BCrypt hash.</summary>
  public bool Verify(string password, string hash)
  {
    return BCrypt.Net.BCrypt.Verify(password, hash);
  }
}