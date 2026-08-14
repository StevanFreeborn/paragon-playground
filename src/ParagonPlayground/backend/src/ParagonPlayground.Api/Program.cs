using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

using ParagonPlayground.Api.Auth;
using ParagonPlayground.Api.Endpoints;
using ParagonPlayground.Api.Middleware;
using ParagonPlayground.Api.Options;
using ParagonPlayground.Api.Services;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.AddSingleton(static sp =>
{
  var opts = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
  return new MongoDbContext(opts.ConnectionString, opts.DatabaseName);
});

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<CookieService>();
builder.Services.AddSingleton<OrganizationRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<SessionRepository>();
builder.Services.AddSingleton<StorageItemRepository>();
builder.Services.AddSingleton<UserCredentialRepository>();
builder.Services.AddSingleton<OrganizationIntegrationRepository>();
builder.Services.AddSingleton<SyncHierarchyIngestor>();

builder.Services.Configure<ParagonOptions>(builder.Configuration.GetSection(ParagonOptions.SectionName));

builder.Services.AddHttpClient<ParagonApiClient>()
  .AddStandardResilienceHandler();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<NotAuthenticatedExceptionHandler>();

builder.Services
  .AddAuthentication(SessionAuthDefaults.Scheme)
  .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
    SessionAuthDefaults.Scheme,
    _ => { }
  );

builder.Services
  .AddAuthorizationBuilder()
  .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(RoleNames.Admin));

builder.Services.Configure<ForwardedHeadersOptions>(static options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api/auth").MapAuthEndpoints();
app.MapGroup("/api/paragon").MapParagonEndpoints();
app.MapGroup("/api/integration").MapIntegrationEndpoints();
app.MapGroup("/api/storage").MapStorageEndpoints();

app.Run();