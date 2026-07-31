using ParagonPlayground.Domain.Entities;

namespace ParagonPlayground.Api.Context;

internal static class HttpContextExtensions
{
  internal static void SetSession(this HttpContext context, Session? session)
  {
    context.Items[ContextKeys.Session] = session;
  }

  internal static Session? GetSession(this HttpContext context)
  {
    return context.Items[ContextKeys.Session] as Session;
  }

  internal static void SetSessionToken(this HttpContext context, string? token)
  {
    context.Items[ContextKeys.SessionToken] = token;
  }

  internal static string? GetSessionToken(this HttpContext context)
  {
    return context.Items[ContextKeys.SessionToken] as string;
  }

  internal static void SetUser(this HttpContext context, User? user)
  {
    context.Items[ContextKeys.User] = user;
  }

  internal static User GetUser(this HttpContext context)
  {
    return context.Items[ContextKeys.User] as User
      ?? throw new NotAuthenticatedException("The authenticated user is not available for this request.");
  }

  internal static bool TryGetUser(this HttpContext context, out User? user)
  {
    user = context.Items[ContextKeys.User] as User;
    return user is not null;
  }

  internal static void SetOrganization(this HttpContext context, Organization? org)
  {
    context.Items[ContextKeys.Organization] = org;
  }

  internal static Organization GetOrganization(this HttpContext context)
  {
    return context.Items[ContextKeys.Organization] as Organization
      ?? throw new NotAuthenticatedException("The authenticated organization is not available for this request.");
  }

  internal static bool TryGetOrganization(this HttpContext context, out Organization? organization)
  {
    organization = context.Items[ContextKeys.Organization] as Organization;
    return organization is not null;
  }
}