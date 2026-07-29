using System.Security.Cryptography;
using System.Text;

namespace ParagonPlayground.Api.Infrastructure;

internal static class TokenHelper
{
  internal static string GenerateToken()
  {
    return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
  }

  internal static string HashToken(string token)
  {
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
  }
}
