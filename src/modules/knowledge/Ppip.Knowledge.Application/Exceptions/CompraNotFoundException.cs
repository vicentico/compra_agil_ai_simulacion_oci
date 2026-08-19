namespace Ppip.Knowledge.Application.Exceptions;

/// <summary>UC-005 precondición ("Compra con chunks indexados") — 404 en docs/06-api/01-example-rag-query.md.</summary>
public sealed class CompraNotFoundException(string compraAgilId) : Exception($"La Compra Ágil {compraAgilId} no existe.")
{
    public string CompraAgilId { get; } = compraAgilId;
}
