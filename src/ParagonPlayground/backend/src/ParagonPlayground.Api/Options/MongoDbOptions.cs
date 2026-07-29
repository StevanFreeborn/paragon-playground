namespace ParagonPlayground.Api.Options;

internal class MongoDbOptions
{
  public const string SectionName = "MongoDb";
  public string ConnectionString { get; set; } = "mongodb://localhost:27017";
  public string DatabaseName { get; set; } = "paragon_playground";
}