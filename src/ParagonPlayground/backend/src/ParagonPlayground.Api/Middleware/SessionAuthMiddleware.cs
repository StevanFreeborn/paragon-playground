using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Infrastructure;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

namespace ParagonPlayground.Api.Middleware;

internal class SessionAuthMiddleware(RequestDelegate next)
{
  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(
    HttpContext context,
    SessionRepository sessionRepo,
    UserRepository userRepo,
    OrganizationRepository orgRepo,
    CookieService cookieService
  )
  {
    var ct = context.RequestAborted;
    var sessionToken = cookieService.GetSessionToken(context);

    if (string.IsNullOrEmpty(sessionToken) is false)
    {
      var tokenHash = TokenHelper.HashToken(sessionToken);
      var session = await sessionRepo.GetByTokenHashAsync(tokenHash, ct);

      if (session is not null && session.ExpiresAt > DateTime.UtcNow)
      {
        context.SetSession(session);
        context.SetSessionToken(sessionToken);

        var user = await userRepo.GetByIdAsync(session.UserId, ct);

        if (user is not null)
        {
          context.SetUser(user);

          var org = await orgRepo.GetByIdAsync(user.OrganizationId, ct);

          if (org is not null)
          {
            context.SetOrganization(org);
          }
        }
      }
    }

    await _next(context);
  }

}