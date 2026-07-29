using Microsoft.AspNetCore.HttpOverrides;

using ParagonPlayground.Api.Endpoints;
using ParagonPlayground.Api.Middleware;
using ParagonPlayground.Api.Options;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.AddSingleton(static sp =>
{
  var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
  return new MongoDbContext(opts.ConnectionString, opts.DatabaseName);
});

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<CookieService>();
builder.Services.AddSingleton<OrganizationRepository>();
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<SessionRepository>();

builder.Services.Configure<ForwardedHeadersOptions>(static options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<SessionAuthMiddleware>();
app.MapGroup("/api/auth").MapAuthEndpoints();

app.Run();