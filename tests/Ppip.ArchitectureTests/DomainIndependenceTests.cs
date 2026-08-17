using System.Reflection;
using NetArchTest.Rules;
using Ppip.BuildingBlocks.Domain;
using Ppip.BuildingBlocks.Messaging;
using Ppip.Procurement.Domain;
using Xunit;

namespace Ppip.ArchitectureTests;

/// <summary>
/// NFR-013: "Dominio independiente de infraestructura... Architecture tests
/// (.NET) validan las dependencias entre capas." El dominio de cada contexto
/// no puede conocer MongoDB/RabbitMQ/Redis/HTTP/ASP.NET Core — eso vive en
/// adaptadores de infraestructura (FASE 5+), nunca en el dominio.
/// </summary>
public class DomainIndependenceTests
{
    private static readonly string[] ForbiddenInfraNamespaces =
    [
        "MongoDB",
        "RabbitMQ",
        "StackExchange.Redis",
        "Microsoft.AspNetCore",
        "Npgsql",
        "System.Net.Http",
        "Docker",
    ];

    public static TheoryData<string, Assembly> DomainAssemblies => new()
    {
        { "Ppip.BuildingBlocks.Domain", typeof(Entity<>).Assembly },
        { "Ppip.BuildingBlocks.Messaging", typeof(OutboxMessage).Assembly },
        { "Ppip.Procurement.Domain", typeof(CompraAgil).Assembly },
    };

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void DomainAssembly_DoesNotDependOnInfrastructure(string assemblyName, Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(ForbiddenInfraNamespaces)
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage(assemblyName, result));
    }

    [Fact]
    public void ProcurementDomain_DoesNotDependOnMessagingBuildingBlock()
    {
        // La traducción a EventEnvelope/OutboxMessage es responsabilidad de
        // la capa de aplicación (todavía no existe) — el dominio solo
        // levanta IDomainEvent puros.
        var result = Types.InAssembly(typeof(CompraAgil).Assembly)
            .Should()
            .NotHaveDependencyOn("Ppip.BuildingBlocks.Messaging")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage("Ppip.Procurement.Domain", result));
    }

    private static string FailureMessage(string assemblyName, TestResult result) =>
        $"{assemblyName} viola NFR-013. Tipos con dependencia prohibida: " +
        string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []);
}
