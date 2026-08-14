using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using ParagonPlayground.Api.Options;
using ParagonPlayground.Domain.DTOs;

namespace ParagonPlayground.Api.Services;

internal sealed class ParagonApiClient
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };
  private readonly HttpClient _httpClient;
  private readonly string _signingKey;
  public string ProjectId { get; }

  public ParagonApiClient(HttpClient httpClient, IOptions<ParagonOptions> options)
  {
    _httpClient = httpClient;
    var paragonOptions = options.Value;
    ProjectId = paragonOptions.ProjectId;
    _signingKey = paragonOptions.SigningKey;
    _httpClient.BaseAddress = new Uri(paragonOptions.ProxyBaseUrl.TrimEnd('/') + "/");
  }


  public bool IsConfigured =>
    string.IsNullOrEmpty(ProjectId) is false
    && string.IsNullOrEmpty(_signingKey) is false;

  public string GenerateToken(string organizationId, string? credentialId = null)
  {
    if (IsConfigured is false)
    {
      throw new InvalidOperationException("Paragon is not configured. Set Paragon:ProjectId and Paragon:SigningKey.");
    }

    using var rsa = RSA.Create();
    rsa.ImportFromPem(_signingKey);

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
      new Claim("aud", $"useparagon.com/{ProjectId}"),
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

  public async Task<string> GetDriveItemNameAsync(
    string jwt,
    string? credentialId,
    string siteId,
    string folderId,
    CancellationToken ct
  )
  {
    var url = $"projects/{ProjectId}/sdk/proxy/sharepoint/sites/{siteId}/drive/items/{folderId}";

    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    _ = request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");

    if (string.IsNullOrEmpty(credentialId) is false)
    {
      _ = request.Headers.TryAddWithoutValidation("X-Paragon-Credential", credentialId);
    }

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    using var doc = JsonDocument.Parse(json);

    if (doc.RootElement.TryGetProperty("output", out var output) && output.TryGetProperty("name", out var nameProp))
    {
      return nameProp.GetString() ?? "Synced SharePoint Folder";
    }

    return "Synced SharePoint Folder";
  }

  public async Task<SyncedRecordsResponse> PullSyncedRecordsAsync(
    string jwt,
    string syncId,
    string? cursor,
    int pageSize,
    CancellationToken ct
  )
  {
    var url = $"https://sync.useparagon.com/api/syncs/{syncId}/records?pageSize={pageSize}";

    if (string.IsNullOrEmpty(cursor) is false)
    {
      url += $"&cursor={Uri.EscapeDataString(cursor)}";
    }

    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    return JsonSerializer.Deserialize<SyncedRecordsResponse>(json, JsonOptions) ?? new SyncedRecordsResponse();
  }

  public async Task<string> UploadFileAsync(
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
    var url = $"projects/{ProjectId}/sdk/proxy/sharepoint"
      + $"/sites/{siteId}/drive/root:/{folderPath.Trim('/')}/{fileName}:/content";

    using var ms = new MemoryStream();
    await fileStream.CopyToAsync(ms, ct).ConfigureAwait(false);
    ms.Position = 0;

    using var request = new HttpRequestMessage(HttpMethod.Put, url)
    {
      Content = new StreamContent(ms),
    };

    _ = request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
    _ = request.Headers.TryAddWithoutValidation("X-Paragon-Credential", credentialId);
    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
  }

  public async Task<Stream> DownloadFileAsync(
    string jwt,
    string credentialId,
    string siteId,
    string driveItemId,
    CancellationToken ct
  )
  {
    var url = $"projects/{ProjectId}/sdk/proxy/sharepoint"
      + $"/sites/{siteId}/drive/items/{driveItemId}/content";

    using var request = new HttpRequestMessage(HttpMethod.Get, url);

    _ = request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
    _ = request.Headers.TryAddWithoutValidation("X-Paragon-Credential", credentialId);
    _ = request.Headers.TryAddWithoutValidation("X-Paragon-Use-Raw-Response", "1");

    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
  }

  public async Task<Stream> DownloadSyncedRecordContentAsync(
    string jwt,
    string syncId,
    string recordId,
    CancellationToken ct
  )
  {
    var url = $"https://sync.useparagon.com/api/syncs/{syncId}/records/{recordId}/content";
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
  }

  public async Task<string> ResolveSiteUrlAsync(
    string jwt,
    string credentialId,
    string siteUrl,
    CancellationToken ct
  )
  {
    var uri = new Uri(siteUrl);
    var segments = uri.AbsolutePath.TrimEnd('/')
      .Split('/', StringSplitOptions.RemoveEmptyEntries);
    var encodedPath = segments.Length > 0
      ? ":/" + string.Join("/", segments.Select(Uri.EscapeDataString))
      : "";

    var proxyUrl = $"projects/{ProjectId}/sdk/proxy/sharepoint/sites/{uri.Host}{encodedPath}";

    using var request = new HttpRequestMessage(HttpMethod.Get, proxyUrl);
    _ = request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
    _ = request.Headers.TryAddWithoutValidation("X-Paragon-Credential", credentialId);

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    using var doc = JsonDocument.Parse(json);

    var output = doc.RootElement.GetProperty("output");
    var idProp = output.GetProperty("id");
    return idProp.GetString() ?? "";
  }

  public async Task<string> EnableSyncAsync(
    string jwt,
    string? credentialId,
    string folderId,
    string siteId,
    CancellationToken ct
  )
  {
    var url = "https://sync.useparagon.com/api/syncs";
    using var request = new HttpRequestMessage(HttpMethod.Post, url);

    request.Headers.Authorization = new("Bearer", jwt);

    var payload = new
    {
      integration = "sharepoint",
      pipeline = "files",
      credentialId,
      configuration = new
      {
        folderId,
        siteId,
      },
    };

    request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();

    using var doc = JsonDocument.Parse(json);
    return doc.RootElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("Failed to obtain sync ID");
  }

  public async Task DeleteSyncAsync(string jwt, string syncId, CancellationToken ct)
  {
    var url = $"https://sync.useparagon.com/api/syncs/{syncId}";
    using var request = new HttpRequestMessage(HttpMethod.Delete, url);
    request.Headers.Authorization = new("Bearer", jwt);

    using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    _ = response.EnsureSuccessStatusCode();
  }
}