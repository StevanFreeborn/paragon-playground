using ParagonPlayground.Api.Context;
using ParagonPlayground.Infrastructure.Data;

namespace ParagonPlayground.Api.Middleware;

internal class TenantResolutionMiddleware(RequestDelegate next)
{
  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context, OrganizationRepository orgRepo)
  {
    if (context.Request.Headers.TryGetValue("X-Organization-Slug", out var slug))
    {
      var org = await orgRepo.GetBySlugAsync(slug.ToString(), context.RequestAborted);

      if (org is not null)
      {
        context.SetOrganization(org);
      }
    }

    await _next(context);
  }
}