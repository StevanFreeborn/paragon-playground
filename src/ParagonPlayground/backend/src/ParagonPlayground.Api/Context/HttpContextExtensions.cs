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

  internal static User? GetUser(this HttpContext context)
  {
    return context.Items[ContextKeys.User] as User;
  }

  internal static void SetOrganization(this HttpContext context, Organization? org)
  {
    context.Items[ContextKeys.Organization] = org;
  }

  internal static Organization? GetOrganization(this HttpContext context)
  {
    return context.Items[ContextKeys.Organization] as Organization;
  }
}
