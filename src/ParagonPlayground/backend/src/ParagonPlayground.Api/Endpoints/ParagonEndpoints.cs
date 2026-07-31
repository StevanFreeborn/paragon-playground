using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Services;
using ParagonPlayground.Domain.DTOs;
using ParagonPlayground.Infrastructure.Data;

namespace ParagonPlayground.Api.Endpoints;

internal static class ParagonEndpoints
{
  internal static RouteGroupBuilder MapParagonEndpoints(this RouteGroupBuilder group)
  {
    _ = group.MapGet("/token", GenerateToken);
    _ = group.RequireAuthorization();
    return group;
  }

  private static async Task<IResult> GenerateToken(
    HttpContext context,
    ParagonService paragon,
    UserCredentialRepository credRepo,
    CancellationToken ct
  )
  {
    var user = context.GetUser();
    var org = context.GetOrganization();

    if (paragon.IsConfigured is false)
    {
      return Results.Problem(
        detail: "Paragon integration is not configured. Set Paragon:ProjectId and Paragon:SigningKey.",
        statusCode: StatusCodes.Status400BadRequest
      );
    }

    var credentials = await credRepo.GetByUserIdAsync(user.Id, ct);
    var spCredential = credentials.FirstOrDefault(c => c.IntegrationType.Equals("sharepoint", StringComparison.OrdinalIgnoreCase));

    var jwt = paragon.GenerateToken(org.Id, spCredential?.CredentialId);

    return Results.Ok(new ParagonTokenResponse
    {
      ParagonJwt = jwt,
      ProjectId = paragon.ProjectId,
    });
  }
}