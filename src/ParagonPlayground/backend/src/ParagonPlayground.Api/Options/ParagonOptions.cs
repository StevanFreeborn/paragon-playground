namespace ParagonPlayground.Api.Options;

internal class ParagonOptions
{
  public const string SectionName = "Paragon";

  public string ProjectId { get; set; } = string.Empty;

  public string SigningKey { get; set; } = string.Empty;

  public string ProxyBaseUrl { get; set; } = "https://proxy.useparagon.com";
}
