using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for user data access.</summary>
public class UserRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Finds a user by their email address.</summary>
  public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
  {
    return await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>Finds a user by their unique identifier.</summary>
  public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken)
  {
    return await _context.Users
      .Find(u => u.Id == id)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Creates a new user in the database.</summary>
  public async Task CreateAsync(User user, CancellationToken cancellationToken)
  {
    await _context.Users
      .InsertOneAsync(user, new InsertOneOptions(), cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Replaces an existing user document.</summary>
  public async Task ReplaceAsync(User user, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(user);

    _ = await _context.Users
      .ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }
}