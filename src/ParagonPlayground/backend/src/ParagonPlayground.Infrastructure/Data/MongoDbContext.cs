using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>MongoDB data access context.</summary>
public sealed class MongoDbContext : IDisposable
{
  private readonly MongoClient _client;
  private readonly IMongoDatabase _database;

  /// <summary>Organizations collection.</summary>
  public IMongoCollection<Organization> Organizations =>
      _database.GetCollection<Organization>("Organizations");

  /// <summary>Users collection.</summary>
  public IMongoCollection<User> Users =>
      _database.GetCollection<User>("Users");

  /// <summary>Sessions collection.</summary>
  public IMongoCollection<Session> Sessions =>
      _database.GetCollection<Session>("Sessions");

  /// <summary>Initializes a new MongoDbContext and connects to the specified database.</summary>
  public MongoDbContext(string connectionString, string databaseName)
  {
    _client = new MongoClient(connectionString);
    _database = _client.GetDatabase(databaseName);
  }

  /// <summary>Releases the underlying MongoClient.</summary>
  public void Dispose()
  {
    _client.Dispose();
    GC.SuppressFinalize(this);
  }

}