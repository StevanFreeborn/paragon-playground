using MongoDB.Bson;

using ParagonPlayground.Api.Auth;
using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Services;
using ParagonPlayground.Domain.DTOs;
using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;

namespace ParagonPlayground.Api.Endpoints;

internal static class IntegrationEndpoints
{
  internal static RouteGroupBuilder MapIntegrationEndpoints(this RouteGroupBuilder group)
  {
    _ = group.MapGet("/config", GetConfig);
    _ = group.MapPut("/config", PutConfig).RequireAdmin();
    _ = group.MapGet("/credentials", GetCredentials);
    _ = group.MapGet("/credentials/org", GetOrgCredentials).RequireAdmin();
    _ = group.MapPost("/credentials", PostCredential);
    _ = group.MapDelete("/credentials/org", PurgeOrgCredentials).RequireAdmin();
    _ = group.MapDelete("/credentials/{credentialId}", DeleteCredential);
    _ = group.RequireAuthorization();
    return group;
  }

  private static async Task<IResult> GetConfig(
    HttpContext context,
    OrganizationIntegrationRepository repo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var config = await repo.GetByOrganizationIdAsync(org.Id, ct);

    if (config is null)
    {
      return Results.Problem(detail: "Integration configuration not found", statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(new IntegrationConfigResponse
    {
      Id = config.Id,
      OrganizationId = config.OrganizationId,
      ConnectionMode = config.ConnectionMode,
      SharePointSiteUrl = config.SharePointSiteUrl,
      SharePointSiteId = config.SharePointSiteId,
      SharePointFolderPath = config.SharePointFolderPath,
      UpdatedAt = config.UpdatedAt,
    });
  }

  private static async Task<IResult> PutConfig(
    IntegrationConfigRequest request,
    HttpContext context,
    OrganizationIntegrationRepository repo,
    UserCredentialRepository credRepo,
    ParagonApiClient paragon,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    var config = await repo.GetByOrganizationIdAsync(org.Id, ct);

    config ??= new OrganizationIntegration
    {
      Id = ObjectId.GenerateNewId().ToString(),
      OrganizationId = org.Id,
    };

    config.ConnectionMode = request.ConnectionMode;
    config.SharePointFolderPath = request.SharePointFolderPath?.Trim();
    config.UpdatedAt = DateTime.UtcNow;

    if (string.IsNullOrWhiteSpace(request.SharePointSiteUrl) is false)
    {
      config.SharePointSiteUrl = request.SharePointSiteUrl.Trim();

      var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
      var spCredential = credentials.FirstOrDefault(c =>
        c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase)
      );

      if (spCredential is not null && paragon.IsConfigured)
      {
        var jwt = paragon.GenerateToken(org.Id, spCredential.CredentialId);

        config.SharePointSiteId = await paragon.ResolveSiteUrlAsync(
          jwt,
          spCredential.CredentialId,
          config.SharePointSiteUrl,
          ct
        );
      }
    }

    await repo.UpsertAsync(config, ct);

    return Results.Ok(new IntegrationConfigResponse
    {
      Id = config.Id,
      OrganizationId = config.OrganizationId,
      ConnectionMode = config.ConnectionMode,
      SharePointSiteUrl = config.SharePointSiteUrl,
      SharePointSiteId = config.SharePointSiteId,
      SharePointFolderPath = config.SharePointFolderPath,
      UpdatedAt = config.UpdatedAt,
    });
  }

  private static async Task<IResult> GetCredentials(
    HttpContext context,
    UserCredentialRepository repo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();

    var credentials = await repo.GetByUserIdAsync(user.Id, ct);

    return Results.Ok(credentials.Select(c => new CredentialResponse
    {
      Id = c.Id,
      CredentialId = c.CredentialId,
      IntegrationType = c.IntegrationType,
      ConnectedAt = c.ConnectedAt,
    }));
  }

  private static async Task<IResult> GetOrgCredentials(
    HttpContext context,
    UserCredentialRepository repo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    var credentials = await repo.GetByOrganizationIdAsync(org.Id, ct);

    return Results.Ok(credentials.Select(c => new CredentialResponse
    {
      Id = c.Id,
      CredentialId = c.CredentialId,
      IntegrationType = c.IntegrationType,
      ConnectedAt = c.ConnectedAt,
    }));
  }

  private static async Task<IResult> PostCredential(
    CredentialRequest request,
    HttpContext context,
    UserCredentialRepository repo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    var credential = new UserCredential
    {
      Id = ObjectId.GenerateNewId().ToString(),
      UserId = user.Id,
      OrganizationId = org.Id,
      CredentialId = request.CredentialId,
      IntegrationType = request.IntegrationType,
      ConnectedAt = DateTime.UtcNow,
    };

    await repo.CreateAsync(credential, ct);

    return Results.Created($"/api/integration/credentials/{credential.Id}", new CredentialResponse
    {
      Id = credential.Id,
      CredentialId = credential.CredentialId,
      IntegrationType = credential.IntegrationType,
      ConnectedAt = credential.ConnectedAt,
    });
  }

  private static async Task<IResult> PurgeOrgCredentials(
    HttpContext context,
    UserCredentialRepository repo,
    CancellationToken ct
  )
  {
    var org = context.GetOrganization();

    _ = await repo.DeleteByOrganizationIdAsync(org.Id, ct);

    return Results.NoContent();
  }

  private static async Task<IResult> DeleteCredential(
    string credentialId,
    HttpContext context,
    UserCredentialRepository repo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();

    var deleted = await repo.DeleteByCredentialIdAsync(credentialId, user.Id, ct);

    if (deleted is false)
    {
      return Results.Problem(detail: "Credential not found", statusCode: StatusCodes.Status404NotFound);
    }

    return Results.NoContent();
  }
}