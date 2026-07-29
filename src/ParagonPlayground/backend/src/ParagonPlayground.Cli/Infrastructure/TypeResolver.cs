using Spectre.Console.Cli;

namespace ParagonPlayground.Cli.Infrastructure;

internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
  public object? Resolve(Type? type)
  {
    return type is null ? null : provider.GetService(type);
  }
}