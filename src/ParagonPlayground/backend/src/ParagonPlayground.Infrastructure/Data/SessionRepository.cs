using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for session data access.</summary>
public class SessionRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Finds a session by its token hash.</summary>
  public async Task<Session?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
  {
    return await _context.Sessions
      .Find(s => s.TokenHash == tokenHash)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Creates a new session in the database.</summary>
  public async Task CreateAsync(Session session, CancellationToken cancellationToken)
  {
    await _context.Sessions
      .InsertOneAsync(session, new InsertOneOptions(), cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Deletes a session by its unique identifier.</summary>
  public async Task DeleteAsync(string id, CancellationToken cancellationToken)
  {
    _ = await _context.Sessions
      .DeleteOneAsync(s => s.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Deletes all sessions for a given user.</summary>
  public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken)
  {
    _ = await _context.Sessions
      .DeleteManyAsync(s => s.UserId == userId, cancellationToken)
      .ConfigureAwait(false);
  }
}