using Microsoft.Extensions.Configuration;

namespace ParagonPlayground.Cli;

internal static class CliConfiguration
{
  public static (string ConnectionString, string DatabaseName) GetMongoDbConfig()
  {
    var config = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
      .AddEnvironmentVariables()
      .Build();

    var connString = config.GetSection("MongoDb")["ConnectionString"]
      ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
      ?? "mongodb://localhost:27017";

    var dbName = config.GetSection("MongoDb")["DatabaseName"]
      ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE")
      ?? "paragon_playground";

    return (connString, dbName);
  }
}