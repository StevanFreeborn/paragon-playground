using MongoDB.Bson;

using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Infrastructure;
using ParagonPlayground.Domain.DTOs;
using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

namespace ParagonPlayground.Api.Endpoints;

internal static class AuthEndpoints
{
  private static readonly TimeSpan SessionDuration = TimeSpan.FromDays(30);

  internal static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
  {
    _ = group.MapPost("/login", LoginAsync);
    _ = group.MapPost("/logout", LogoutAsync);
    _ = group.MapGet("/me", Me);
    return group;
  }

  private static async Task<IResult> LoginAsync(
    LoginRequest request,
    UserRepository userRepo,
    OrganizationRepository orgRepo,
    SessionRepository sessionRepo,
    PasswordService passwordService,
    CookieService cookieService,
    HttpContext context,
    CancellationToken ct
  )
  {
    ArgumentNullException.ThrowIfNull(request);

    var user = await userRepo.GetByEmailAsync(request.Email, ct);

    if (user is null || passwordService.Verify(request.Password, user.PasswordHash) is false)
    {
      return Results.Json(new { error = "Invalid email or password" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var org = await orgRepo.GetByIdAsync(user.OrganizationId, ct);

    var sessionToken = TokenHelper.GenerateToken();
    var session = new Session
    {
      Id = ObjectId.GenerateNewId().ToString(),
      UserId = user.Id,
      TokenHash = TokenHelper.HashToken(sessionToken),
      CreatedAt = DateTime.UtcNow,
      ExpiresAt = DateTime.UtcNow.Add(SessionDuration),
    };

    await sessionRepo.CreateAsync(session, ct);

    var xsrfToken = TokenHelper.GenerateToken();

    cookieService.SetSessionCookie(context, sessionToken, SessionDuration);
    cookieService.SetXsrfCookie(context, xsrfToken, SessionDuration);

    return Results.Ok(new UserResponse
    {
      Id = user.Id,
      Email = user.Email,
      DisplayName = user.DisplayName,
      OrganizationId = org?.Id ?? string.Empty,
      OrganizationName = org?.Name ?? string.Empty,
      OrganizationSlug = org?.Slug ?? string.Empty,
    });
  }

  private static async Task<IResult> LogoutAsync(
    SessionRepository sessionRepo,
    CookieService cookieService,
    HttpContext context,
    CancellationToken ct
  )
  {
    var session = context.GetSession();

    if (session is not null)
    {
      await sessionRepo.DeleteAsync(session.Id, ct);
    }

    cookieService.ClearSessionCookie(context);
    cookieService.ClearXsrfCookie(context);

    return Results.Ok(new { message = "Logged out" });
  }

  private static IResult Me(HttpContext context)
  {
    var user = context.GetUser();

    if (user is null)
    {
      return Results.Json(new { error = "Not authenticated" }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var org = context.GetOrganization();

    return Results.Ok(new UserResponse
    {
      Id = user.Id,
      Email = user.Email,
      DisplayName = user.DisplayName,
      OrganizationId = org?.Id ?? string.Empty,
      OrganizationName = org?.Name ?? string.Empty,
      OrganizationSlug = org?.Slug ?? string.Empty,
    });
  }

}
