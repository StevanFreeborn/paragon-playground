using System.Text.Json;

using MongoDB.Bson;

using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Services;
using ParagonPlayground.Domain.DTOs;
using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;

namespace ParagonPlayground.Api.Endpoints;

internal static class StorageEndpoints
{
  private static readonly Action<ILogger, Exception?> LogWebhookTestPing =
    LoggerMessage.Define(
      LogLevel.Information,
      new EventId(1, nameof(HandleParagonWebhook)),
      "Received Paragon Webhook test/verification ping."
    );

  private static readonly Action<ILogger, string, Exception?> LogNoRootItemFoundWarning =
    LoggerMessage.Define<string>(
      LogLevel.Warning,
      new EventId(2, nameof(HandleParagonWebhook)),
      "No root item found in DB for syncId: {SyncId}"
    );

  private static readonly Action<ILogger, string, string?, string?, Exception?> LogSyncErrored =
    LoggerMessage.Define<string, string?, string?>(
      LogLevel.Error,
      new EventId(3, nameof(HandleParagonWebhook)),
      "Sync {SyncId} failed with code {Code}: {Message}"
    );

  private static readonly Action<ILogger, string, Exception?> LogUnhandledWebhookEvent =
    LoggerMessage.Define<string>(
      LogLevel.Information,
      new EventId(4, nameof(HandleParagonWebhook)),
      "Received unhandled Paragon event type: {Event}"
    );

  internal static RouteGroupBuilder MapStorageEndpoints(this RouteGroupBuilder group)
  {
    _ = group.MapGet("/", ListItems);
    _ = group.MapPost("/folders", CreateFolder);
    _ = group.MapPost("/files", UploadFile);
    _ = group.MapDelete("/{id}", DeleteItem);
    _ = group.MapGet("/{id}/download", GetDownloadUrls);
    _ = group.MapGet("/{id}/content", ProxyContent);
    _ = group.MapPost("/synced-folders", CreateSyncedFolder);
    _ = group.MapPost("/webhook/paragon", HandleParagonWebhook).AllowAnonymous();
    _ = group.RequireAuthorization();
    return group;
  }

  private static async Task<IResult> HandleParagonWebhook(
    ParagonWebhookPayload payload,
    StorageItemRepository repo,
    UserCredentialRepository credRepo,
    ParagonApiClient paragon,
    SyncHierarchyIngestor syncHierarchyIngestor,
    ILoggerFactory loggerFactory,
    CancellationToken ct
  )
  {
    var logger = loggerFactory.CreateLogger("ParagonWebhook");

    if (payload is null || string.IsNullOrEmpty(payload.SyncInstanceId) || payload.Event is "test" or "ping")
    {
      LogWebhookTestPing(logger, null);
      return Results.Ok(new { status = "ok", message = "Webhook endpoint active" });
    }

    var rootItem = await repo.GetByManagedSyncIdAsync(payload.SyncInstanceId, ct);

    if (rootItem is null)
    {
      LogNoRootItemFoundWarning(logger, payload.SyncInstanceId, null);
      return Results.Ok(new { status = "ignored", reason = "Sync instance not tracked in app" });
    }

    var jwt = paragon.GenerateToken(rootItem.OrganizationId);

    switch (payload.Event)
    {
      case "sync_complete":
      case "record_created":
      case "record_updated":
        await syncHierarchyIngestor.IngestSyncRecordsAsync(jwt, payload.SyncInstanceId, rootItem, ct);
        break;

      case "record_deleted":
        if (!string.IsNullOrEmpty(payload.Data?.RecordId))
        {
          await DeleteSyncedRecordAsync(payload.Data.RecordId, rootItem, repo, ct);
        }
        break;

      case "sync_errored":
        LogSyncErrored(logger, payload.SyncInstanceId, payload.Error?.Code, payload.Error?.Message, null);
        break;

      default:
        LogUnhandledWebhookEvent(logger, payload.Event, null);
        break;
    }

    return Results.Ok(new { status = "processed" });
  }

  private static async Task DeleteSyncedRecordAsync(
    string recordId,
    StorageItem rootItem,
    StorageItemRepository repo,
    CancellationToken ct
  )
  {
    StorageItem? existing = null;

    if (string.IsNullOrWhiteSpace(rootItem.ManagedSyncId) is false)
    {
      existing = await repo.GetByParagonRecordIdAsync(
        rootItem.OrganizationId,
        rootItem.ManagedSyncId,
        recordId,
        ct
      );
    }

    existing ??= await repo.GetBySharePointDriveItemIdAsync(rootItem.OrganizationId, recordId, ct);

    if (existing is not null)
    {
      await repo.DeleteAsync(existing.Id, ct);
    }
  }

  private static async Task<IResult> CreateSyncedFolder(
    CreateSyncedFolderRequest request,
    HttpContext context,
    StorageItemRepository repo,
    UserCredentialRepository credRepo,
    ParagonApiClient paragon,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
    var spCredential = credentials.FirstOrDefault(
      c => c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase)
    );

    var jwt = paragon.GenerateToken(org.Id, spCredential?.CredentialId);

    var folderName = await paragon.GetDriveItemNameAsync(
      jwt,
      spCredential?.CredentialId,
      request.SharePointSiteId,
      request.SharePointFolderId,
      ct
    );

    var syncId = await paragon.EnableSyncAsync(
      jwt,
      spCredential?.CredentialId,
      request.SharePointFolderId,
      request.SharePointSiteId,
      ct
    );

    var rootFolder = new StorageItem
    {
      Id = ObjectId.GenerateNewId().ToString(),
      OrganizationId = org.Id,
      Name = folderName,
      IsFolder = true,
      ParentId = request.ParentId,
      IsManagedSync = true,
      ManagedSyncId = syncId,
      SharePointFolderId = request.SharePointFolderId,
      SharePointSiteId = request.SharePointSiteId,
      IsReadOnly = true,
      CreatedByUserId = user.Id,
      CreatedAt = DateTime.UtcNow
    };

    await repo.CreateAsync(rootFolder, ct);

    return Results.Created($"/api/storage/{rootFolder.Id}", rootFolder);
  }

  private static async Task<IResult> ListItems(
    string? parentId,
    HttpContext context,
    StorageItemRepository repo,
    UserRepository userRepo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var items = await repo.GetByParentIdAsync(org.Id, parentId, ct);
    var userIds = items.Select(i => i.CreatedByUserId).Distinct().ToList();
    var users = new Dictionary<string, string>();

    foreach (var uid in userIds)
    {
      var u = await userRepo.GetByIdAsync(uid, ct);
      users[uid] = u?.DisplayName ?? "Unknown";
    }

    return Results.Ok(items.Select(i => StorageItemResponse.From(i, users.GetValueOrDefault(i.CreatedByUserId, "Unknown"))));
  }

  private static async Task<IResult> CreateFolder(
    CreateFolderRequest request,
    HttpContext context,
    StorageItemRepository repo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    if (string.IsNullOrWhiteSpace(request.Name))
    {
      return Results.Problem(detail: "Folder name is required", statusCode: StatusCodes.Status400BadRequest);
    }

    var item = new StorageItem
    {
      Id = ObjectId.GenerateNewId().ToString(),
      OrganizationId = org.Id,
      Name = request.Name.Trim(),
      IsFolder = true,
      ParentId = request.ParentId,
      CreatedByUserId = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    await repo.CreateAsync(item, ct);

    return Results.Created($"/api/storage/{item.Id}", StorageItemResponse.From(item, user.DisplayName));
  }

  private static async Task<IResult> UploadFile(
    HttpContext context,
    StorageItemRepository storageRepo,
    UserCredentialRepository credRepo,
    OrganizationIntegrationRepository configRepo,
    ParagonApiClient paragon,
    UserRepository userRepo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    var file = context.Request.Form.Files.Count > 0 ? context.Request.Form.Files[0] : null;

    if (file is null || file.Length is 0)
    {
      return Results.Problem(detail: "File is required", statusCode: StatusCodes.Status400BadRequest);
    }

    var parentId = (string?)context.Request.Form["parentId"];

    var config = await configRepo.GetByOrganizationIdAsync(org.Id, ct);

    if (config is null || string.IsNullOrWhiteSpace(config.SharePointSiteId))
    {
      return Results.Problem(
        detail: "SharePoint integration is not configured. Ask an admin to set it up.",
        statusCode: StatusCodes.Status400BadRequest
      );
    }

    var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
    var spCredential = credentials.FirstOrDefault(c =>
      c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase)
    );

    if (spCredential is null)
    {
      return Results.Problem(
        detail: "No SharePoint credential found. Connect to SharePoint first.",
        statusCode: StatusCodes.Status400BadRequest
      );
    }

    if (paragon.IsConfigured is false)
    {
      return Results.Problem(
        detail: "Paragon integration is not configured on the server.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    var jwt = paragon.GenerateToken(org.Id, spCredential.CredentialId);

    string sharePointResponse;

    await using (var stream = file.OpenReadStream())
    {
      sharePointResponse = await paragon.UploadFileAsync(
        jwt,
        spCredential.CredentialId,
        config.SharePointSiteId,
        config.SharePointFolderPath ?? "",
        file.FileName,
        stream,
        file.ContentType,
        ct
      );
    }

    string? driveItemId = null;
    string? webUrl = null;

    try
    {
      using var doc = JsonDocument.Parse(sharePointResponse);

      if (doc.RootElement.TryGetProperty("output", out var output))
      {
        driveItemId = output.TryGetProperty("id", out var id) ? id.GetString() : null;
        webUrl = output.TryGetProperty("webUrl", out var wu) ? wu.GetString() : null;
      }
    }
    catch (JsonException)
    {
      // Response parsing is best-effort; store what we have
    }

    var storageItem = new StorageItem
    {
      Id = ObjectId.GenerateNewId().ToString(),
      OrganizationId = org.Id,
      Name = file.FileName,
      IsFolder = false,
      ParentId = parentId,
      ContentType = file.ContentType,
      FileSize = file.Length,
      SharePointSiteId = config.SharePointSiteId,
      SharePointDriveItemId = driveItemId,
      SharePointWebUrl = webUrl,
      CreatedByUserId = user.Id,
      CreatedAt = DateTime.UtcNow,
    };

    await storageRepo.CreateAsync(storageItem, ct);

    return Results.Created($"/api/storage/{storageItem.Id}", StorageItemResponse.From(storageItem, user.DisplayName));
  }

  private static async Task<IResult> DeleteItem(
    string id,
    HttpContext context,
    StorageItemRepository repo,
    UserCredentialRepository credRepo,
    ParagonApiClient paragon,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();
    var item = await repo.GetByIdAsync(id, ct);

    if (item is null)
    {
      return Results.NotFound();
    }

    if (item.IsReadOnly && !item.IsManagedSync)
    {
      return Results.Problem(detail: "Contents of a managed sync folder are read-only.", statusCode: StatusCodes.Status403Forbidden);
    }

    if (item.IsManagedSync && !string.IsNullOrEmpty(item.ManagedSyncId))
    {
      var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
      var spCredential = credentials.FirstOrDefault(c => c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase));
      var jwt = paragon.GenerateToken(org.Id, spCredential?.CredentialId);

      await paragon.DeleteSyncAsync(jwt, item.ManagedSyncId, ct);

      await DeleteFolderRecursiveAsync(item.Id, repo, ct);
      return Results.NoContent();
    }

    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
  }

  private static async Task DeleteFolderRecursiveAsync(
    string folderId,
    StorageItemRepository repo,
    CancellationToken ct
  )
  {
    var children = await repo.GetChildrenAsync(folderId, ct);

    foreach (var child in children)
    {
      if (child.IsFolder)
      {
        await DeleteFolderRecursiveAsync(child.Id, repo, ct);
      }
      else
      {
        await repo.DeleteAsync(child.Id, ct);
      }
    }

    await repo.DeleteAsync(folderId, ct);
  }

  private static async Task<IResult> GetDownloadUrls(
    string id,
    HttpContext context,
    StorageItemRepository repo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var item = await repo.GetByIdAsync(id, ct);

    if (item is null || item.OrganizationId != org.Id || item.IsFolder)
    {
      return Results.Problem(detail: "Item not found", statusCode: StatusCodes.Status404NotFound);
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

    return Results.Ok(new DownloadResponse
    {
      SharePointUrl = item.SharePointWebUrl,
      ProxyUrl = $"{baseUrl}/api/storage/{item.Id}/content",
    });
  }

  private static async Task<IResult> ProxyContent(
    string id,
    HttpContext context,
    StorageItemRepository storageRepo,
    UserCredentialRepository credRepo,
    ParagonApiClient paragon,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var item = await storageRepo.GetByIdAsync(id, ct);

    if (item is null || item.OrganizationId != org.Id || item.IsFolder)
    {
      return Results.Problem(detail: "Item not found", statusCode: StatusCodes.Status404NotFound);
    }

    if (paragon.IsConfigured is false)
    {
      return Results.Problem(
        detail: "Paragon integration is not configured.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    Stream fileStream;

    if (
      string.IsNullOrWhiteSpace(item.ManagedSyncId) is false
      && string.IsNullOrWhiteSpace(item.ParagonRecordId) is false
    )
    {
      var jwt = paragon.GenerateToken(org.Id);

      fileStream = await paragon.DownloadSyncedRecordContentAsync(
        jwt,
        item.ManagedSyncId,
        item.ParagonRecordId,
        ct
      );
    }
    else
    {
      if (string.IsNullOrEmpty(item.SharePointDriveItemId) || string.IsNullOrEmpty(item.SharePointSiteId))
      {
        return Results.Problem(
          detail: "No SharePoint reference available for this file.",
          statusCode: StatusCodes.Status400BadRequest
        );
      }

      var user = context.GetUser();
      var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
      var spCredential = credentials.FirstOrDefault(c =>
        c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase)
      );

      if (spCredential is null)
      {
        return Results.Problem(
          detail: "No SharePoint credential found.",
          statusCode: StatusCodes.Status400BadRequest
        );
      }

      var jwt = paragon.GenerateToken(org.Id, spCredential.CredentialId);

      fileStream = await paragon.DownloadFileAsync(
        jwt,
        spCredential.CredentialId,
        item.SharePointSiteId,
        item.SharePointDriveItemId,
        ct
      );
    }

    return Results.Stream(fileStream, item.ContentType ?? "application/octet-stream", item.Name);
  }
}