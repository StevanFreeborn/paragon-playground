using MongoDB.Bson;

using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ParagonPlayground.Cli.Commands;

internal class SeedCommand(
  OrganizationRepository orgRepo,
  UserRepository userRepo,
  PasswordService passwordService) : AsyncCommand<SeedCommand.Settings>
{
  internal class Settings : CommandSettings { }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    var org = await orgRepo.GetBySlugAsync("acme", cancellationToken);

    if (org is null)
    {
      org = new Organization
      {
        Id = ObjectId.GenerateNewId().ToString(),
        Name = "Acme Corp",
        Slug = "acme",
        CreatedAt = DateTime.UtcNow,
      };

      await orgRepo.CreateAsync(org, cancellationToken);

      AnsiConsole.MarkupLine($"[green]Created org: {org.Name} (slug: {org.Slug})[/]");
    }
    else
    {
      AnsiConsole.MarkupLine($"[yellow]Org 'acme' already exists[/]");
    }

    var users = new[]
    {
      (Email: "alice@acme.com", Name: "Alice", Password: "password123", Role: "admin"),
      (Email: "bob@acme.com", Name: "Bob", Password: "password123", Role: "member"),
    };

    foreach (var (email, name, password, role) in users)
    {
      var existing = await userRepo.GetByEmailAsync(email, cancellationToken);

      if (existing is null)
      {
        var user = new User
        {
          Id = ObjectId.GenerateNewId().ToString(),
          Email = email,
          DisplayName = name,
          PasswordHash = passwordService.Hash(password),
          OrganizationId = org.Id,
          Role = role,
          CreatedAt = DateTime.UtcNow,
        };

        await userRepo.CreateAsync(user, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Created user: {user.Email} ({user.DisplayName})[/]");
      }
      else
      {
        if (existing.Role != role)
        {
          existing.Role = role;
          await userRepo.ReplaceAsync(existing, cancellationToken);
          AnsiConsole.MarkupLine($"[yellow]User '{email}' role updated to '{role}'[/]");
        }
        else
        {
          AnsiConsole.MarkupLine($"[yellow]User '{email}' already exists[/]");
        }
      }
    }

    AnsiConsole.MarkupLine("[bold green]Seed complete![/]");

    return 0;
  }
}