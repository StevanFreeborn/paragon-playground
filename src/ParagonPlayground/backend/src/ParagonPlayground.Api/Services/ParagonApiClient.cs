using System.Net.Http.Headers;
using System.Text.Json;

using Microsoft.Extensions.Options;

using ParagonPlayground.Api.Options;

namespace ParagonPlayground.Api.Services;

internal sealed class ParagonApiClient
{
  private readonly HttpClient _httpClient;
  private readonly string _projectId;

  public ParagonApiClient(HttpClient httpClient, IOptions<ParagonOptions> options)
  {
    _httpClient = httpClient;
    _projectId = options.Value.ProjectId;
    _httpClient.BaseAddress = new Uri(options.Value.ProxyBaseUrl.TrimEnd('/') + "/");
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
    var url = $"projects/{_projectId}/sdk/proxy/sharepoint"
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
    var url = $"projects/{_projectId}/sdk/proxy/sharepoint"
      + $"/sites/{siteId}/drive/items/{driveItemId}/content";

    using var request = new HttpRequestMessage(HttpMethod.Get, url);

    _ = request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {jwt}");
    _ = request.Headers.TryAddWithoutValidation("X-Paragon-Credential", credentialId);

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

    var proxyUrl = $"projects/{_projectId}/sdk/proxy/sharepoint/sites/{uri.Host}{encodedPath}";

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
}