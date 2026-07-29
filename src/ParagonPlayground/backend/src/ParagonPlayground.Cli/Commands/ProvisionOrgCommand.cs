using System.ComponentModel;

using MongoDB.Bson;

using ParagonPlayground.Domain.Entities;
using ParagonPlayground.Infrastructure.Data;

using Spectre.Console;
using Spectre.Console.Cli;

namespace ParagonPlayground.Cli.Commands;

internal class ProvisionOrgCommand(OrganizationRepository orgRepo) : AsyncCommand<ProvisionOrgCommand.Settings>
{
  internal class Settings : CommandSettings
  {
    [Description("Organization name")]
    [CommandOption("-n|--name")]
    public required string Name { get; set; }

    [Description("Organization slug (used as subdomain)")]
    [CommandOption("-s|--slug")]
    public required string Slug { get; set; }
  }

  protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
  {
    var existing = await orgRepo.GetBySlugAsync(settings.Slug, cancellationToken);

    if (existing is not null)
    {
      AnsiConsole.MarkupLine($"[yellow]Organization with slug '{settings.Slug}' already exists (Id: {existing.Id})[/]");
      return 0;
    }

    var org = new Organization
    {
      Id = ObjectId.GenerateNewId().ToString(),
      Name = settings.Name,
      Slug = settings.Slug,
      CreatedAt = DateTime.UtcNow,
    };

    await orgRepo.CreateAsync(org, cancellationToken);

    AnsiConsole.MarkupLine($"[green]Organization '{org.Name}' created with slug '{org.Slug}' (Id: {org.Id})[/]");

    return 0;
  }
}