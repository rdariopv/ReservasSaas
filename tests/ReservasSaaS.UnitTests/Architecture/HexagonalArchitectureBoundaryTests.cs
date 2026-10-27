using System.Reflection;
using FluentAssertions;
using ReservasSaaS.Application.UseCases;
using ReservasSaaS.Domain.Common;
using Xunit;

namespace ReservasSaaS.UnitTests.Architecture;

public class HexagonalArchitectureBoundaryTests
{
    [Fact]
    public void Domain_MustNotReference_AnyOtherLayer()
    {
        // Arrange
        var domainAssembly = typeof(IAggregateRoot).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        // Assert - Domain is the pure core
        referencedAssemblies.Should().NotContain("ReservasSaaS.Application");
        referencedAssemblies.Should().NotContain("ReservasSaaS.Infrastructure");
        referencedAssemblies.Should().NotContain("ReservasSaaS.Api");
    }

    [Fact]
    public void Application_MustNotReference_InfrastructureOrApi()
    {
        // Arrange
        var applicationAssembly = typeof(GetSystemHealthUseCase).Assembly;
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        // Assert - Application only knows Domain
        referencedAssemblies.Should().NotContain("ReservasSaaS.Infrastructure");
        referencedAssemblies.Should().NotContain("ReservasSaaS.Api");
    }
}
