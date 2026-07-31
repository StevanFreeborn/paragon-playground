using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for organization data access.</summary>
public class OrganizationRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Finds an organization by its URL slug.</summary>
  public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
  {
    return await _context.Organizations
      .Find(o => o.Slug == slug)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Finds an organization by its unique identifier.</summary>
  public async Task<Organization?> GetByIdAsync(string id, CancellationToken cancellationToken)
  {
    return await _context.Organizations
      .Find(o => o.Id == id)
      .FirstOrDefaultAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  /// <summary>Creates a new organization in the database.</summary>
  public async Task CreateAsync(Organization organization, CancellationToken cancellationToken)
  {
    await _context.Organizations
      .InsertOneAsync(organization, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }
}