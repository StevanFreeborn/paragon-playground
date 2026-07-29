namespace ParagonPlayground.Domain.DTOs;

/// <summary>Login credentials submitted by the user.</summary>
public class LoginRequest
{
  /// <summary>User's email address.</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>User's plain-text password.</summary>
  public string Password { get; set; } = string.Empty;
}