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
  internal static RouteGroupBuilder MapStorageEndpoints(this RouteGroupBuilder group)
  {
    _ = group.MapGet("/", ListItems);
    _ = group.MapPost("/folders", CreateFolder);
    _ = group.MapPost("/files", UploadFile);
    _ = group.MapDelete("/{id}", DeleteItem);
    _ = group.MapGet("/{id}/download", GetDownloadUrls);
    _ = group.MapGet("/{id}/content", ProxyContent);
    _ = group.RequireAuthorization();
    return group;
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

    return Results.Ok(items.Select(i => new StorageItemResponse
    {
      Id = i.Id,
      Name = i.Name,
      IsFolder = i.IsFolder,
      ParentId = i.ParentId,
      ContentType = i.ContentType,
      FileSize = i.FileSize,
      SharePointWebUrl = i.SharePointWebUrl,
      CreatedByUserId = i.CreatedByUserId,
      CreatedByDisplayName = users.GetValueOrDefault(i.CreatedByUserId, "Unknown"),
      CreatedAt = i.CreatedAt,
    }));
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

    return Results.Created($"/api/storage/{item.Id}", new StorageItemResponse
    {
      Id = item.Id,
      Name = item.Name,
      IsFolder = true,
      ParentId = item.ParentId,
      CreatedByUserId = item.CreatedByUserId,
      CreatedByDisplayName = user.DisplayName,
      CreatedAt = item.CreatedAt,
    });
  }

  private static async Task<IResult> UploadFile(
    HttpContext context,
    StorageItemRepository storageRepo,
    UserCredentialRepository credRepo,
    OrganizationIntegrationRepository configRepo,
    ParagonService paragon,
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

    return Results.Created($"/api/storage/{storageItem.Id}", new StorageItemResponse
    {
      Id = storageItem.Id,
      Name = storageItem.Name,
      IsFolder = false,
      ParentId = storageItem.ParentId,
      ContentType = storageItem.ContentType,
      FileSize = storageItem.FileSize,
      SharePointWebUrl = storageItem.SharePointWebUrl,
      CreatedByUserId = storageItem.CreatedByUserId,
      CreatedByDisplayName = user.DisplayName,
      CreatedAt = storageItem.CreatedAt,
    });
  }

  private static async Task<IResult> DeleteItem(
    string id,
    HttpContext context,
    StorageItemRepository repo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var item = await repo.GetByIdAsync(id, ct);

    if (item is null || item.OrganizationId != org.Id)
    {
      return Results.Problem(detail: "Item not found", statusCode: StatusCodes.Status404NotFound);
    }

    await repo.DeleteAsync(id, ct);

    return Results.NoContent();
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
    ParagonService paragon,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    var item = await storageRepo.GetByIdAsync(id, ct);

    if (item is null || item.OrganizationId != org.Id || item.IsFolder)
    {
      return Results.Problem(detail: "Item not found", statusCode: StatusCodes.Status404NotFound);
    }

    if (string.IsNullOrEmpty(item.SharePointDriveItemId) || string.IsNullOrEmpty(item.SharePointSiteId))
    {
      return Results.Problem(
        detail: "No SharePoint reference available for this file.",
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
        detail: "No SharePoint credential found.",
        statusCode: StatusCodes.Status400BadRequest
      );
    }

    if (paragon.IsConfigured is false)
    {
      return Results.Problem(
        detail: "Paragon integration is not configured.",
        statusCode: StatusCodes.Status500InternalServerError
      );
    }

    var jwt = paragon.GenerateToken(org.Id, spCredential.CredentialId);
    var fileStream = await paragon.DownloadFileAsync(
      jwt,
      spCredential.CredentialId,
      item.SharePointSiteId,
      item.SharePointDriveItemId,
      ct
    );

    return Results.Stream(fileStream, item.ContentType ?? "application/octet-stream", item.Name);
  }
}