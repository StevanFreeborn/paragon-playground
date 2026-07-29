using Microsoft.Extensions.DependencyInjection;

using ParagonPlayground.Cli;
using ParagonPlayground.Cli.Commands;
using ParagonPlayground.Cli.Infrastructure;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

using Spectre.Console.Cli;

var services = new ServiceCollection();

var (connString, dbName) = CliConfiguration.GetMongoDbConfig();
services.AddSingleton(_ => new MongoDbContext(connString, dbName));
services.AddSingleton<OrganizationRepository>();
services.AddSingleton<UserRepository>();
services.AddSingleton<PasswordService>();

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(static config =>
{
  _ = config.AddBranch("provision", static provision =>
  {
    _ = provision.AddCommand<ProvisionOrgCommand>("org");
    _ = provision.AddCommand<ProvisionUserCommand>("user");
    _ = provision.AddCommand<SeedCommand>("seed");
  });
});

return app.Run(args);