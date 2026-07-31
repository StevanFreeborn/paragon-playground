using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using ParagonPlayground.Api.Context;
using ParagonPlayground.Api.Infrastructure;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

namespace ParagonPlayground.Api.Auth;

internal sealed class SessionAuthenticationHandler(
  IOptionsMonitor<AuthenticationSchemeOptions> options,
  ILoggerFactory logger,
  UrlEncoder encoder,
  CookieService cookieService,
  SessionRepository sessionRepository,
  UserRepository userRepository,
  OrganizationRepository organizationRepository
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
  private readonly CookieService _cookieService = cookieService;
  private readonly SessionRepository _sessionRepository = sessionRepository;
  private readonly UserRepository _userRepository = userRepository;
  private readonly OrganizationRepository _organizationRepository = organizationRepository;

  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    var sessionToken = _cookieService.GetSessionToken(Context);

    if (string.IsNullOrEmpty(sessionToken))
    {
      return AuthenticateResult.NoResult();
    }

    var tokenHash = TokenHelper.HashToken(sessionToken);
    var session = await _sessionRepository.GetByTokenHashAsync(tokenHash, Context.RequestAborted);

    if (session is null || session.ExpiresAt <= DateTime.UtcNow)
    {
      return AuthenticateResult.Fail("Session token is invalid or expired.");
    }

    var user = await _userRepository.GetByIdAsync(session.UserId, Context.RequestAborted);

    if (user is null)
    {
      return AuthenticateResult.Fail("The user associated with this session no longer exists.");
    }

    var organization = await _organizationRepository.GetByIdAsync(user.OrganizationId, Context.RequestAborted);

    if (organization is null)
    {
      return AuthenticateResult.Fail("The organization associated with this session no longer exists.");
    }

    Context.SetSession(session);
    Context.SetSessionToken(sessionToken);
    Context.SetUser(user);
    Context.SetOrganization(organization);

    var identity = new ClaimsIdentity(
      [
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.DisplayName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(ClaimNames.OrganizationId, organization.Id),
        new Claim(ClaimNames.OrganizationName, organization.Name),
        new Claim(ClaimNames.OrganizationSlug, organization.Slug),
      ],
      Scheme.Name
    );

    return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
  }
}