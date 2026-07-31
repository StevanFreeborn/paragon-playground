using System.ComponentModel;

using MongoDB.Bson;

using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;
using ParagonPlayground.Infrastructure.Services;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ParagonPlayground.Cli.Commands;

internal class ProvisionUserCommand(
  OrganizationRepository orgRepo,
  UserRepository userRepo,
  PasswordService passwordService
) : AsyncCommand<ProvisionUserCommand.Settings>
{
  internal class Settings : CommandSettings
  {
    [Description("User email address")]
    [CommandOption("-e|--email")]
    public required string Email { get; set; }

    [Description("User password")]
    [CommandOption("-p|--password")]
    public required string Password { get; set; }

    [Description("Display name")]
    [CommandOption("-n|--name")]
    public required string Name { get; set; }

    [Description("Organization slug")]
    [CommandOption("-o|--org-slug")]
    public required string OrgSlug { get; set; }

    [Description("User role (admin or member)")]
    [CommandOption("-r|--role")]
    public string Role { get; set; } = "member";
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    var org = await orgRepo.GetBySlugAsync(settings.OrgSlug, cancellationToken);

    if (org is null)
    {
      AnsiConsole.MarkupLine($"[red]Organization with slug '{settings.OrgSlug}' not found. Create it first with 'provision org'.[/]");
      return 1;
    }

    var existing = await userRepo.GetByEmailAsync(settings.Email, cancellationToken);

    if (existing is not null)
    {
      AnsiConsole.MarkupLine($"[yellow]User with email '{settings.Email}' already exists (Id: {existing.Id})[/]");
      return 0;
    }

    var user = new User
    {
      Id = ObjectId.GenerateNewId().ToString(),
      Email = settings.Email,
      DisplayName = settings.Name,
      PasswordHash = passwordService.Hash(settings.Password),
      OrganizationId = org.Id,
      Role = settings.Role,
      CreatedAt = DateTime.UtcNow,
    };

    await userRepo.CreateAsync(user, cancellationToken);

    AnsiConsole.MarkupLine($"[green]User '{user.Email}' created in org '{org.Name}' (Id: {user.Id})[/]");

    return 0;
  }
}