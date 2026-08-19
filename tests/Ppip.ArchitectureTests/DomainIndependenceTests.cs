using System.Reflection;
using NetArchTest.Rules;
using Ppip.BuildingBlocks.Domain;
using Ppip.BuildingBlocks.Messaging;
using Ppip.DocumentIntelligence.Application;
using Ppip.Knowledge.Application;
using Ppip.Procurement.Domain;
using Xunit;
using DocumentAggregate = Ppip.DocumentIntelligence.Domain.Document;
using KnowledgeAggregate = Ppip.Knowledge.Domain.Embedding;

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
        { "Ppip.DocumentIntelligence.Domain", typeof(DocumentAggregate).Assembly },
        { "Ppip.Knowledge.Domain", typeof(KnowledgeAggregate).Assembly },
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

    /// <summary>
    /// A diferencia de Ppip.Procurement.Application (que depende de
    /// Infrastructure a propósito desde FASE 5, para IChileCompraClient),
    /// Ppip.Document.Application NO tiene ese precedente: todos sus puertos
    /// viven en Domain/Ports desde el diseño inicial (FASE 7). Esta regla
    /// blinda esa decisión — si alguien agrega una referencia a
    /// Infrastructure ahí, este test debe fallar.
    /// </summary>
    [Fact]
    public void DocumentApplication_DoesNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(DocumentDownloadOrchestrator).Assembly)
            .Should()
            .NotHaveDependencyOn("Ppip.DocumentIntelligence.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage("Ppip.DocumentIntelligence.Application", result));
    }

    /// <summary>
    /// Igual criterio que <see cref="DocumentApplication_DoesNotDependOnInfrastructure"/>:
    /// Ppip.Knowledge.Application reusa deliberadamente Domain/Ports de otros
    /// contextos (DocumentIntelligence, Procurement — ADR-012), pero nunca
    /// Infrastructure de ningún módulo, ni siquiera el propio.
    /// </summary>
    [Fact]
    public void KnowledgeApplication_DoesNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(typeof(RagQueryOrchestrator).Assembly)
            .Should()
            .NotHaveDependencyOnAny("Ppip.Knowledge.Infrastructure", "Ppip.DocumentIntelligence.Infrastructure", "Ppip.Procurement.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, FailureMessage("Ppip.Knowledge.Application", result));
    }

    private static string FailureMessage(string assemblyName, TestResult result) =>
        $"{assemblyName} viola NFR-013. Tipos con dependencia prohibida: " +
        string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []);
}
