using Microsoft.AspNetCore.Http;

namespace ParagonPlayground.Infrastructure.Services;

/// <summary>Manages session and XSRF cookies.</summary>
public class CookieService
{
  private const string SessionCookieName = "session";
  private const string XsrfCookieName = "XSRF-TOKEN";
  private const string XsrfHeaderName = "X-XSRF-Token";

  /// <summary>Sets the HttpOnly session cookie.</summary>
  public void SetSessionCookie(HttpContext context, string token, TimeSpan expiresIn)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.Response.Cookies.Append(SessionCookieName, token, new CookieOptions
    {
      HttpOnly = true,
      Secure = context.Request.IsHttps,
      SameSite = SameSiteMode.Strict,
      Path = "/",
      MaxAge = expiresIn,
    });
  }

  /// <summary>Sets the client-accessible XSRF token cookie.</summary>
  public void SetXsrfCookie(HttpContext context, string token, TimeSpan expiresIn)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.Response.Cookies.Append(XsrfCookieName, token, new CookieOptions
    {
      HttpOnly = false,
      Secure = context.Request.IsHttps,
      SameSite = SameSiteMode.Strict,
      Path = "/",
      MaxAge = expiresIn,
    });
  }

  /// <summary>Clears the session cookie.</summary>
  public void ClearSessionCookie(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.Response.Cookies.Delete(SessionCookieName);
  }

  /// <summary>Clears the XSRF token cookie.</summary>
  public void ClearXsrfCookie(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.Response.Cookies.Delete(XsrfCookieName);
  }

  /// <summary>Reads the session token from the request cookie.</summary>
  public string? GetSessionToken(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return context.Request.Cookies[SessionCookieName];
  }

  /// <summary>Reads the XSRF token from the request cookie.</summary>
  public string? GetXsrfCookieValue(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return context.Request.Cookies[XsrfCookieName];
  }

  /// <summary>Reads the XSRF token from the request header.</summary>
  public string? GetXsrfHeaderValue(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    return context.Request.Headers[XsrfHeaderName];
  }

  /// <summary>Validates that the XSRF cookie matches the XSRF header.</summary>
  public bool ValidateXsrf(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var cookieToken = GetXsrfCookieValue(context);
    var headerToken = GetXsrfHeaderValue(context);

    return string.IsNullOrEmpty(cookieToken) is false
        && string.IsNullOrEmpty(headerToken) is false
        && cookieToken == headerToken;
  }
}