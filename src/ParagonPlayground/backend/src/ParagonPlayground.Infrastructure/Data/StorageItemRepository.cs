using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for storage item (file/folder) data access.</summary>
public class StorageItemRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Lists items in a folder (or root items when parentId is null).</summary>
  public async Task<List<StorageItem>> GetByParentIdAsync(
    string organizationId, string? parentId, CancellationToken ct)
  {
    var filter = Builders<StorageItem>.Filter.Eq(i => i.OrganizationId, organizationId)
      & Builders<StorageItem>.Filter.Eq(i => i.ParentId, parentId);

    return await _context.StorageItems.Find(filter)
      .SortByDescending(i => i.IsFolder)
      .ThenBy(i => i.Name)
      .ToListAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Finds a storage item by ID.</summary>
  public async Task<StorageItem?> GetByIdAsync(string id, CancellationToken ct)
  {
    return await _context.StorageItems.Find(i => i.Id == id)
      .FirstOrDefaultAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Creates a new storage item.</summary>
  public async Task CreateAsync(StorageItem item, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(item);

    await _context.StorageItems
      .InsertOneAsync(item, cancellationToken: ct)
      .ConfigureAwait(false);
  }

  /// <summary>Deletes a storage item by ID.</summary>
  public async Task DeleteAsync(string id, CancellationToken ct)
  {
    _ = await _context.StorageItems
      .DeleteOneAsync(i => i.Id == id, ct)
      .ConfigureAwait(false);
  }
}
