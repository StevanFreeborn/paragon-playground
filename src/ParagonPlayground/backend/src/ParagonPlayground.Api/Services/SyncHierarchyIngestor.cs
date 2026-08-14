using MongoDB.Bson;

using ParagonPlayground.Domain.DTOs;
using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;

namespace ParagonPlayground.Api.Services;

internal sealed class SyncHierarchyIngestor(StorageItemRepository repo, ParagonApiClient paragon)
{
  private readonly StorageItemRepository _repo = repo;
  private readonly ParagonApiClient _paragon = paragon;

  public async Task IngestSyncRecordsAsync(
    string jwt,
    string syncId,
    StorageItem rootItem,
    CancellationToken ct
  )
  {
    var fetchedRecords = await FetchRecordsAsync(jwt, syncId, ct);
    var normalizedRecords = NormalizeRecords(fetchedRecords, rootItem);

    if (normalizedRecords.All.Count == 0)
    {
      return;
    }

    var existingState = await LoadExistingStateAsync(rootItem.OrganizationId, normalizedRecords.ExternalIds, ct);
    var folderResult = await PersistFoldersAsync(rootItem, syncId, normalizedRecords.Folders, existingState, ct);
    await PersistUnresolvedFoldersAtRootAsync(rootItem, syncId, folderResult.UnresolvedFolders, existingState, ct);
    await PersistFilesAsync(rootItem, syncId, normalizedRecords.Files, folderResult.FolderStorageIdByExternalId, existingState, ct);
  }

  private async Task<IReadOnlyList<SyncedRecordItem>> FetchRecordsAsync(
    string jwt,
    string syncId,
    CancellationToken ct
  )
  {
    string? cursor = null;
    var hasMore = true;
    var records = new List<SyncedRecordItem>();

    while (hasMore && !ct.IsCancellationRequested)
    {
      var response = await _paragon.PullSyncedRecordsAsync(jwt, syncId, cursor, 100, ct);
      records.AddRange(response.Data);

      cursor = response.Paging?.Cursor;
      hasMore = (response.Paging?.RemainingRecords ?? 0) > 0 && !string.IsNullOrEmpty(cursor);
    }

    return records;
  }

  private static NormalizedRecordSet NormalizeRecords(
    IReadOnlyList<SyncedRecordItem> records,
    StorageItem rootItem
  )
  {
    var all = records
      .Where(r => string.IsNullOrWhiteSpace(r.ExternalId) is false)
      .GroupBy(r => r.ExternalId, StringComparer.Ordinal)
      .Select(g => g.Last())
      .Where(r => IsRootRecord(r, rootItem) is false)
      .ToList();

    return new NormalizedRecordSet(
      All: all,
      Folders: [.. all.Where(r => r.IsFolder())],
      Files: [.. all.Where(r => r.IsFolder() is false)],
      ExternalIds: [.. all.Select(r => r.ExternalId).Distinct(StringComparer.Ordinal)]
    );
  }

  private async Task<ExistingStorageState> LoadExistingStateAsync(
    string organizationId,
    IReadOnlyList<string> externalIds,
    CancellationToken ct
  )
  {
    var existingItems = await _repo.GetBySharePointDriveItemIdsAsync(organizationId, externalIds, ct);
    return ExistingStorageState.FromItems(existingItems);
  }

  private async Task<FolderResolutionResult> PersistFoldersAsync(
    StorageItem rootItem,
    string syncId,
    IReadOnlyList<SyncedRecordItem> folders,
    ExistingStorageState state,
    CancellationToken ct
  )
  {
    var pending = folders.ToList();
    var folderStorageIdByExternalId = new Dictionary<string, string>(state.FolderStorageIdByExternalId, StringComparer.Ordinal);

    while (pending.Count > 0 && !ct.IsCancellationRequested)
    {
      var progressMade = false;
      var nextPending = new List<SyncedRecordItem>();

      foreach (var folder in pending)
      {
        if (TryResolveParentId(rootItem, folder, folderStorageIdByExternalId, out var parentId) is false)
        {
          nextPending.Add(folder);
          continue;
        }

        var persisted = await UpsertStorageItemAsync(rootItem, syncId, folder, isFolder: true, parentId, state.ItemsByExternalId, ct);
        folderStorageIdByExternalId[folder.ExternalId] = persisted.Id;
        progressMade = true;
      }

      pending = nextPending;

      if (progressMade is false)
      {
        break;
      }
    }

    return new FolderResolutionResult(folderStorageIdByExternalId, pending);
  }

  private async Task PersistUnresolvedFoldersAtRootAsync(
    StorageItem rootItem,
    string syncId,
    IReadOnlyList<SyncedRecordItem> unresolvedFolders,
    ExistingStorageState state,
    CancellationToken ct
  )
  {
    foreach (var folder in unresolvedFolders)
    {
      var persisted = await UpsertStorageItemAsync(rootItem, syncId, folder, isFolder: true, rootItem.Id, state.ItemsByExternalId, ct);
      state.FolderStorageIdByExternalId[folder.ExternalId] = persisted.Id;
    }
  }

  private async Task PersistFilesAsync(
    StorageItem rootItem,
    string syncId,
    IReadOnlyList<SyncedRecordItem> files,
    Dictionary<string, string> folderStorageIdByExternalId,
    ExistingStorageState state,
    CancellationToken ct
  )
  {
    foreach (var file in files)
    {
      var parentId = rootItem.Id;

      if (
        IsRootParent(rootItem, file) is false
        && folderStorageIdByExternalId.TryGetValue(file.ParentFolderId, out var resolvedParentId)
      )
      {
        parentId = resolvedParentId;
      }

      _ = await UpsertStorageItemAsync(rootItem, syncId, file, isFolder: false, parentId, state.ItemsByExternalId, ct);
    }
  }

  private async Task<StorageItem> UpsertStorageItemAsync(
    StorageItem rootItem,
    string syncId,
    SyncedRecordItem record,
    bool isFolder,
    string parentId,
    Dictionary<string, StorageItem> itemsByExternalId,
    CancellationToken ct
  )
  {
    if (itemsByExternalId.TryGetValue(record.ExternalId, out var existing))
    {
      existing.Name = record.Name;
      existing.IsFolder = isFolder;
      existing.ParentId = parentId;
      existing.ContentType = isFolder ? null : record.MimeType;
      existing.FileSize = isFolder ? 0 : record.Size;
      existing.SharePointSiteId = rootItem.SharePointSiteId;
      existing.SharePointDriveItemId = record.ExternalId;
      existing.SharePointWebUrl = record.Url;
      existing.IsManagedSync = false;
      existing.ManagedSyncId = syncId;
      existing.ParagonRecordId = record.Id;
      existing.IsReadOnly = true;

      await _repo.UpdateAsync(existing, ct);
      return existing;
    }

    var newItem = new StorageItem
    {
      Id = ObjectId.GenerateNewId().ToString(),
      OrganizationId = rootItem.OrganizationId,
      Name = record.Name,
      IsFolder = isFolder,
      ParentId = parentId,
      ContentType = isFolder ? null : record.MimeType,
      FileSize = isFolder ? 0 : record.Size,
      SharePointSiteId = rootItem.SharePointSiteId,
      SharePointDriveItemId = record.ExternalId,
      SharePointWebUrl = record.Url,
      IsManagedSync = false,
      ManagedSyncId = syncId,
      ParagonRecordId = record.Id,
      IsReadOnly = true,
      CreatedByUserId = rootItem.CreatedByUserId,
      CreatedAt = DateTime.UtcNow
    };

    await _repo.CreateAsync(newItem, ct);
    itemsByExternalId[record.ExternalId] = newItem;
    return newItem;
  }

  private static bool TryResolveParentId(
    StorageItem rootItem,
    SyncedRecordItem record,
    Dictionary<string, string> folderStorageIdByExternalId,
    out string parentId
  )
  {
    if (IsRootParent(rootItem, record))
    {
      parentId = rootItem.Id;
      return true;
    }

    if (folderStorageIdByExternalId.TryGetValue(record.ParentFolderId, out var resolvedParentId))
    {
      parentId = resolvedParentId;
      return true;
    }

    parentId = string.Empty;
    return false;
  }

  private static bool IsRootRecord(SyncedRecordItem record, StorageItem rootItem)
  {
    return string.Equals(record.ExternalId, rootItem.SharePointFolderId, StringComparison.OrdinalIgnoreCase)
      || string.Equals(record.ExternalId, rootItem.SharePointDriveItemId, StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsRootParent(StorageItem rootItem, SyncedRecordItem record)
  {
    return string.IsNullOrWhiteSpace(record.ParentFolderId)
      || string.Equals(record.ParentFolderId, rootItem.SharePointFolderId, StringComparison.OrdinalIgnoreCase)
      || string.Equals(record.ParentFolderId, rootItem.SharePointDriveItemId, StringComparison.OrdinalIgnoreCase);
  }

  private sealed record NormalizedRecordSet(
    IReadOnlyList<SyncedRecordItem> All,
    IReadOnlyList<SyncedRecordItem> Folders,
    IReadOnlyList<SyncedRecordItem> Files,
    IReadOnlyList<string> ExternalIds
  );

  private sealed class ExistingStorageState
  {
    public Dictionary<string, StorageItem> ItemsByExternalId { get; }
    public Dictionary<string, string> FolderStorageIdByExternalId { get; }

    private ExistingStorageState(
      Dictionary<string, StorageItem> itemsByExternalId,
      Dictionary<string, string> folderStorageIdByExternalId
    )
    {
      ItemsByExternalId = itemsByExternalId;
      FolderStorageIdByExternalId = folderStorageIdByExternalId;
    }

    public static ExistingStorageState FromItems(IEnumerable<StorageItem> items)
    {
      var itemList = items.ToList();

      var itemsByExternalId = itemList
        .Where(i => string.IsNullOrWhiteSpace(i.SharePointDriveItemId) is false)
        .ToDictionary(i => i.SharePointDriveItemId!, StringComparer.Ordinal);

      var folderStorageIdByExternalId = itemList
        .Where(i => i.IsFolder && string.IsNullOrWhiteSpace(i.SharePointDriveItemId) is false)
        .ToDictionary(i => i.SharePointDriveItemId!, i => i.Id, StringComparer.Ordinal);

      return new ExistingStorageState(itemsByExternalId, folderStorageIdByExternalId);
    }
  }

  private sealed record FolderResolutionResult(
    Dictionary<string, string> FolderStorageIdByExternalId,
    IReadOnlyList<SyncedRecordItem> UnresolvedFolders
  );
}
