namespace ParagonPlayground.Api.Auth;

internal static class PolicyNames
{
  internal const string AdminOnly = "AdminOnly";
}

internal static class RoleNames
{
  internal const string Admin = "admin";
}

internal static class SessionAuthDefaults
{
  internal const string Scheme = "Session";
}

internal static class ClaimNames
{
  internal const string OrganizationId = "OrganizationId";

  internal const string OrganizationName = "OrganizationName";

  internal const string OrganizationSlug = "OrganizationSlug";
}

internal static class EndpointAuthorizationExtensions
{
  internal static RouteHandlerBuilder RequireAdmin(this RouteHandlerBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    _ = builder.RequireAuthorization(PolicyNames.AdminOnly);
    return builder;
  }
}