using MongoDB.Driver;

using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Infrastructure.Data;

/// <summary>Repository for storage item (file/folder) data access.</summary>
public class StorageItemRepository(MongoDbContext context)
{
  private readonly MongoDbContext _context = context;

  /// <summary>Lists items in a folder (or root items when parentId is null).</summary>
  public async Task<List<StorageItem>> GetByParentIdAsync(
    string organizationId,
    string? parentId,
    CancellationToken ct
  )
  {
    var filter = Builders<StorageItem>.Filter.Eq(i => i.OrganizationId, organizationId)
      & Builders<StorageItem>.Filter.Eq(i => i.ParentId, parentId);

    return await _context.StorageItems.Find(filter)
      .SortByDescending(i => i.IsFolder)
      .ThenBy(i => i.Name)
      .ToListAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Lists all direct child items of a given parent folder.</summary>
  public async Task<List<StorageItem>> GetChildrenAsync(string parentId, CancellationToken ct)
  {
    return await _context.StorageItems.Find(i => i.ParentId == parentId)
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

  /// <summary>Finds a root storage item by Paragon Managed Sync ID.</summary>
  public async Task<StorageItem?> GetByManagedSyncIdAsync(string managedSyncId, CancellationToken ct)
  {
    return await _context.StorageItems.Find(i => i.ManagedSyncId == managedSyncId)
      .FirstOrDefaultAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Finds a storage item by organization ID and SharePoint Drive Item ID.</summary>
  public async Task<StorageItem?> GetBySharePointDriveItemIdAsync(
    string organizationId,
    string sharePointDriveItemId,
    CancellationToken ct
  )
  {
    var filter = Builders<StorageItem>.Filter.Eq(i => i.OrganizationId, organizationId)
      & Builders<StorageItem>.Filter.Eq(i => i.SharePointDriveItemId, sharePointDriveItemId);

    return await _context.StorageItems.Find(filter)
      .FirstOrDefaultAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Finds a synced storage item by organization, sync ID, and Paragon record ID.</summary>
  public async Task<StorageItem?> GetByParagonRecordIdAsync(
    string organizationId,
    string managedSyncId,
    string paragonRecordId,
    CancellationToken ct
  )
  {
    var filter = Builders<StorageItem>.Filter.Eq(i => i.OrganizationId, organizationId)
      & Builders<StorageItem>.Filter.Eq(i => i.ManagedSyncId, managedSyncId)
      & Builders<StorageItem>.Filter.Eq(i => i.ParagonRecordId, paragonRecordId);

    return await _context.StorageItems.Find(filter)
      .FirstOrDefaultAsync(ct)
      .ConfigureAwait(false);
  }

  /// <summary>Finds storage items by organization ID and SharePoint Drive Item IDs.</summary>
  public async Task<List<StorageItem>> GetBySharePointDriveItemIdsAsync(
    string organizationId,
    IReadOnlyCollection<string> sharePointDriveItemIds,
    CancellationToken ct
  )
  {
    ArgumentNullException.ThrowIfNull(sharePointDriveItemIds);

    if (sharePointDriveItemIds.Count == 0)
    {
      return [];
    }

    var filter = Builders<StorageItem>.Filter.Eq(i => i.OrganizationId, organizationId)
      & Builders<StorageItem>.Filter.In(i => i.SharePointDriveItemId, sharePointDriveItemIds);

    return await _context.StorageItems.Find(filter)
      .ToListAsync(ct)
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

  /// <summary>Updates an existing storage item.</summary>
  public async Task UpdateAsync(StorageItem item, CancellationToken ct)
  {
    ArgumentNullException.ThrowIfNull(item);

    _ = await _context.StorageItems
      .ReplaceOneAsync(i => i.Id == item.Id, item, cancellationToken: ct)
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