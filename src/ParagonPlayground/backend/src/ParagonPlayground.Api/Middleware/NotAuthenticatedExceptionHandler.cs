using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using ParagonPlayground.Api.Context;

namespace ParagonPlayground.Api.Middleware;

internal sealed class NotAuthenticatedExceptionHandler(ILogger<NotAuthenticatedExceptionHandler> logger) : IExceptionHandler
{
  private static readonly Action<ILogger, string, Exception> LogUnauthorizedAccess =
    LoggerMessage.Define<string>(
      LogLevel.Warning,
      new EventId(1, "NotAuthenticatedException"),
      "Session state was accessed without an authenticated request. Path: {Path}"
    );

  private readonly ILogger<NotAuthenticatedExceptionHandler> _logger = logger;

  public ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken
  )
  {
    if (exception is not NotAuthenticatedException)
    {
      return ValueTask.FromResult(false);
    }

    LogUnauthorizedAccess(_logger, httpContext.Request.Path.ToString(), exception);

    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

    if (httpContext.RequestServices.GetService<IProblemDetailsService>() is { } problemDetailsService)
    {
      return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
      {
        HttpContext = httpContext,
        ProblemDetails = new ProblemDetails
        {
          Status = StatusCodes.Status401Unauthorized,
          Title = "Unauthorized",
          Detail = exception.Message,
        },
      });
    }

    return ValueTask.FromResult(false);
  }
}