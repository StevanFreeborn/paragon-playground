using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using ParagonPlayground.Api.Options;

namespace ParagonPlayground.Api.Services;

internal sealed class ParagonService(IOptions<ParagonOptions> options, ParagonApiClient apiClient)
{
  private readonly ParagonOptions _options = options.Value;
  private readonly ParagonApiClient _apiClient = apiClient;

  public string ProjectId => _options.ProjectId;

  public bool IsConfigured =>
    string.IsNullOrEmpty(_options.ProjectId) is false
    && string.IsNullOrEmpty(_options.SigningKey) is false;

  public string GenerateToken(string organizationId, string? credentialId = null)
  {
    if (IsConfigured is false)
    {
      throw new InvalidOperationException("Paragon is not configured. Set Paragon:ProjectId and Paragon:SigningKey.");
    }

    using var rsa = RSA.Create();
    rsa.ImportFromPem(_options.SigningKey);

    var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = "paragon" };
    var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

    var now = DateTime.UtcNow;

    var permissions = credentialId is not null
      ? new Dictionary<string, object>
      {
        ["integration:sharepoint"] = new Dictionary<string, object>
        {
          [$"credential:{credentialId}"] = true,
        },
      }
      : (object)new Dictionary<string, object>
      {
        ["integration:sharepoint"] = new Dictionary<string, object>
        {
          ["credential:*"] = new[] { "credential:write" },
        },
      };

    var claims = new[]
    {
      new Claim("sub", $"org:{organizationId}"),
      new Claim("aud", $"useparagon.com/{_options.ProjectId}"),
      new Claim("urn:useparagon:connect:permissions", JsonSerializer.Serialize(permissions)),
    };

    var token = new JwtSecurityToken(
      claims: claims,
      notBefore: now,
      expires: now.AddHours(1),
      signingCredentials: signingCredentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public Task<string> UploadFileAsync(
    string jwt,
    string credentialId,
    string siteId,
    string folderPath,
    string fileName,
    Stream fileStream,
    string contentType,
    CancellationToken ct
  )
  {
    return _apiClient.UploadFileAsync(jwt, credentialId, siteId, folderPath, fileName, fileStream, contentType, ct);
  }

  public Task<string> ResolveSiteUrlAsync(
    string jwt,
    string credentialId,
    string siteUrl,
    CancellationToken ct
  )
  {
    return _apiClient.ResolveSiteUrlAsync(jwt, credentialId, siteUrl, ct);
  }

  public Task<Stream> DownloadFileAsync(
    string jwt,
    string credentialId,
    string siteId,
    string driveItemId,
    CancellationToken ct
  )
  {
    return _apiClient.DownloadFileAsync(jwt, credentialId, siteId, driveItemId, ct);
  }
}