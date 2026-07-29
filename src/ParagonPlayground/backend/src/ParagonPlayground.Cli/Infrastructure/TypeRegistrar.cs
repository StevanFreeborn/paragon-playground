using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace ParagonPlayground.Cli.Infrastructure;

internal sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
  public ITypeResolver Build()
  {
    return new TypeResolver(services.BuildServiceProvider());
  }

  public void Register(Type service, Type implementation)
  {
    _ = services.AddSingleton(service, implementation);
  }

  public void RegisterInstance(Type service, object implementation)
  {
    _ = services.AddSingleton(service, implementation);
  }

  public void RegisterLazy(Type service, Func<object> factory)
  {
    _ = services.AddSingleton(service, _ => factory());
  }
}