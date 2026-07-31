using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for organization integration configuration data access.</summary>
public class OrganizationIntegrationRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Finds the integration config for an organization.</summary>
  public async Task<OrganizationIntegration?> GetByOrganizationIdAsync(
    string organizationId,
    CancellationToken ct
  )
  {
    return await _context.OrganizationIntegrations
      .Find(c => c.OrganizationId == organizationId)
      .FirstOrDefaultAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Creates or replaces the integration config for an organization.</summary>
  public async Task UpsertAsync(OrganizationIntegration config, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(config);

    var filter = Builders<OrganizationIntegration>.Filter
      .Eq(c => c.OrganizationId, config.OrganizationId);

    var options = new ReplaceOptions() { IsUpsert = true };

    _ = await _context.OrganizationIntegrations
      .ReplaceOneAsync(filter, config, options, ct)
      .ConfigureAwait(false);
  }
}
