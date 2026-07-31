using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for user credential data access.</summary>
public class UserCredentialRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Finds all credentials for a given user.</summary>
  public async Task<List<UserCredential>> GetByUserIdAsync(string userId, CancellationToken ct)
  {
    return await _context.UserCredentials
      .Find(c => c.UserId == userId)
      .ToListAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Finds all credentials for a given organization.</summary>
  public async Task<List<UserCredential>> GetByOrganizationIdAsync(string organizationId, CancellationToken ct)
  {
    return await _context.UserCredentials
      .Find(c => c.OrganizationId == organizationId)
      .ToListAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Stores a new credential mapping.</summary>
  public async Task CreateAsync(UserCredential credential, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(credential);

    await _context.UserCredentials
      .InsertOneAsync(credential, cancellationToken: ct)
      .ConfigureAwait(false);
  }

  /// <summary>Deletes all credentials for a given organization.</summary>
  public async Task<long> DeleteByOrganizationIdAsync(string organizationId, CancellationToken ct)
  {
    var result = await _context.UserCredentials
      .DeleteManyAsync(c => c.OrganizationId == organizationId, ct)
      .ConfigureAwait(false);

    return result.DeletedCount;
  }

  /// <summary>Deletes a credential by its Paragon credential ID for a given user.</summary>
  public async Task<bool> DeleteByCredentialIdAsync(string credentialId, string userId, CancellationToken ct)
  {
    var filter = Builders<UserCredential>.Filter.And(
      Builders<UserCredential>.Filter.Eq(c => c.CredentialId, credentialId),
      Builders<UserCredential>.Filter.Eq(c => c.UserId, userId)
    );

    var result = await _context.UserCredentials
      .DeleteOneAsync(filter, ct)
      .ConfigureAwait(false);

    return result.DeletedCount > 0;
  }
}